using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Backend.DTOs;
using Backend.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Helper
{
    public class DBHelper : IDBHelper
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string _connectionString;

        public DBHelper(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, _configuration["JWT:Issuer"]),
                new Claim(JwtRegisteredClaimNames.Aud, _configuration["JWT:Audience"]),
                new Claim("UserId", user.Id),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("FullName", user.FullName),
            }.Concat(roleClaims);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return await Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }

        public async Task<(string token, string refreshToken)> GenerateTokens(ApplicationUser user)
        {
            // Generate JWT token
            var token = await GenerateJwtToken(user);

            // Generate refresh token
            var refreshToken = GenerateRefreshToken();

            // Save refresh token to user
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30); // 30 days for refresh token
            await _userManager.UpdateAsync(user);

            return (token, refreshToken);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public async Task<bool> ValidateRefreshToken(string refreshToken)
        {
            var user = await GetUserByRefreshToken(refreshToken);
            return user != null && user.RefreshTokenExpiresAt > DateTime.UtcNow;
        }

        public async Task<ApplicationUser?> GetUserByRefreshToken(string refreshToken)
        {
            return await _userManager.Users.FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken
            );
        }

        public async Task<bool> AddUserRole(ApplicationUser user)
        {
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                var result = await _userManager.AddToRoleAsync(user, "User");
                return result.Succeeded;
            }
            return false;
        }

        public async Task<bool> AddAgencyRole(ApplicationUser user)
        {
            if (!await _roleManager.RoleExistsAsync("Agency"))
                await _roleManager.CreateAsync(new IdentityRole("Agency"));

            if (!await _userManager.IsInRoleAsync(user, "Agency"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Agency");
                return result.Succeeded;
            }
            return false;
        }

        public async Task<bool> AddAdminRole(ApplicationUser user)
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Admin");
                return result.Succeeded;
            }
            return false;
        }

        public async Task<bool> IsUserAdmin(ApplicationUser user)
        {
            return await _userManager.IsInRoleAsync(user, "Admin");
        }

        #region Location Operations

        public async Task<IEnumerable<LocationResponseDto>> GetLocations()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT Id, Name, Description, ImageUrl, IsPopular, IsActive, 
                           ClickCount, RequestCallCount, CreatedAt, UpdatedAt,
                           Country, Continent
                    FROM Locations
                    WHERE IsActive = 1
                    ORDER BY Name";

                using var command = new SqlCommand(sql, connection);
                var locations = new List<LocationResponseDto>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    try
                    {
                        locations.Add(MapToLocationResponseDto(reader));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error mapping location: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        continue;
                    }
                }

                return locations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLocations: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<IEnumerable<LocationResponseDto>> GetPopularLocations(int limit = 10)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                SELECT TOP (@Limit) Id, Name, Description, ImageUrl, IsPopular, IsActive, 
                       ClickCount, RequestCallCount, CreatedAt, UpdatedAt,
                       Country, Continent
                FROM Locations
                WHERE IsActive = 1 AND IsPopular = 1
                ORDER BY ClickCount DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Limit", limit);
            var locations = new List<LocationResponseDto>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                locations.Add(MapToLocationResponseDto(reader));
            }

            return locations;
        }

        public async Task<LocationResponseDto> GetLocationById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                SELECT Id, Name, Description, ImageUrl, IsPopular, IsActive, 
                       ClickCount, RequestCallCount, CreatedAt, UpdatedAt,
                       Country, Continent
                FROM Locations
                WHERE Id = @Id AND IsActive = 1";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToLocationResponseDto(reader);
            }

            return null;
        }

        public async Task<LocationResponseDto> CreateLocation(LocationCreateDto location)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                INSERT INTO Locations (Name, Description, ImageUrl, IsPopular, IsActive, CreatedAt, UpdatedAt, ClickCount, RequestCallCount)
                OUTPUT INSERTED.*
                VALUES (@Name, @Description, @ImageUrl, @IsPopular, @IsActive, GETUTCDATE(), GETUTCDATE(), 0, 0)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", location.Name);
            command.Parameters.AddWithValue(
                "@Description",
                (object)location.Description ?? DBNull.Value
            );
            command.Parameters.AddWithValue("@ImageUrl", (object)location.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsPopular", location.IsPopular);
            command.Parameters.AddWithValue("@IsActive", location.IsActive);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToLocationResponseDto(reader);
            }

            return null;
        }

        public async Task<bool> UpdateLocation(int id, LocationUpdateDto location)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                UPDATE Locations
                SET Name = @Name,
                    Description = @Description,
                    ImageUrl = @ImageUrl,
                    IsPopular = @IsPopular,
                    IsActive = @IsActive,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", location.Name);
            command.Parameters.AddWithValue(
                "@Description",
                (object)location.Description ?? DBNull.Value
            );
            command.Parameters.AddWithValue("@ImageUrl", (object)location.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsPopular", location.IsPopular);
            command.Parameters.AddWithValue("@IsActive", location.IsActive);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteLocation(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                UPDATE Locations
                SET IsActive = 0,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> IncrementLocationClickCount(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                UPDATE Locations
                SET ClickCount = ClickCount + 1,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> IncrementLocationRequestCallCount(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                UPDATE Locations
                SET RequestCallCount = RequestCallCount + 1,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        #endregion

        #region Deal Operations

        public async Task<IEnumerable<DealResponseDto>> GetDeals(int? locationId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql =
                    @"
                    SELECT d.Id, d.Title, d.LocationId, d.UserId, d.Price, d.DiscountedPrice, 
                           d.DiscountPercentage, d.Rating, d.DaysCount, d.NightsCount, 
                           d.StartPoint, d.EndPoint, d.Duration, d.Description, d.Photos,
                           d.ElderlyFriendly, d.InternetIncluded, d.TravelIncluded, 
                           d.MealsIncluded, d.SightseeingIncluded, d.StayIncluded,
                           d.AirTransfer, d.RoadTransfer, d.TrainTransfer, 
                           d.TravelCostIncluded, d.GuideIncluded, d.PhotographyIncluded,
                           d.InsuranceIncluded, d.VisaIncluded, d.Itinerary, 
                           d.PackageOptions, d.MapUrl, d.Policies, d.PackageType,
                           d.IsActive, d.CreatedAt, d.UpdatedAt, d.Status,
                           d.ApprovalStatus, d.SearchKeywords,
                           l.Name as LocationName, 
                           l.Description as LocationDescription,
                           l.ImageUrl as LocationImageUrl, 
                           l.IsPopular as LocationIsPopular,
                           l.IsActive as LocationIsActive, 
                           l.ClickCount as LocationClickCount,
                           l.RequestCallCount as LocationRequestCallCount,
                           l.CreatedAt as LocationCreatedAt, 
                           l.UpdatedAt as LocationUpdatedAt,
                           l.Country as LocationCountry,
                           l.Continent as LocationContinent
                    FROM Deals d
                    INNER JOIN Locations l ON d.LocationId = l.Id
                    WHERE d.IsActive = 1";

                if (locationId.HasValue)
                {
                    sql += " AND d.LocationId = @LocationId";
                }

                using var command = new SqlCommand(sql, connection);
                if (locationId.HasValue)
                {
                    command.Parameters.AddWithValue("@LocationId", locationId.Value);
                }

                var deals = new List<DealResponseDto>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    try
                    {
                        deals.Add(MapToDealResponseDto(reader));
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue processing other deals
                        Console.WriteLine($"Error mapping deal: {ex.Message}");
                        continue;
                    }
                }

                return deals;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDeals: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<DealResponseDto> GetDealById(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT d.Id, d.Title, d.LocationId, d.UserId, d.Price, d.DiscountedPrice, 
                           d.DiscountPercentage, d.Rating, d.DaysCount, d.NightsCount, 
                           d.StartPoint, d.EndPoint, d.Duration, d.Description, d.Photos,
                           d.ElderlyFriendly, d.InternetIncluded, d.TravelIncluded, 
                           d.MealsIncluded, d.SightseeingIncluded, d.StayIncluded,
                           d.AirTransfer, d.RoadTransfer, d.TrainTransfer, 
                           d.TravelCostIncluded, d.GuideIncluded, d.PhotographyIncluded,
                           d.InsuranceIncluded, d.VisaIncluded, d.Itinerary, 
                           d.PackageOptions, d.MapUrl, d.Policies, d.PackageType,
                           d.IsActive, d.CreatedAt, d.UpdatedAt, d.Status,
                           d.ApprovalStatus, d.SearchKeywords,
                           l.Name as LocationName, 
                           l.Description as LocationDescription,
                           l.ImageUrl as LocationImageUrl, 
                           l.IsPopular as LocationIsPopular,
                           l.IsActive as LocationIsActive, 
                           l.ClickCount as LocationClickCount,
                           l.RequestCallCount as LocationRequestCallCount,
                           l.CreatedAt as LocationCreatedAt, 
                           l.UpdatedAt as LocationUpdatedAt,
                           l.Country as LocationCountry,
                           l.Continent as LocationContinent
                    FROM Deals d
                    INNER JOIN Locations l ON d.LocationId = l.Id
                    WHERE d.Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    try
                    {
                        return MapToDealResponseDto(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error mapping deal: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        throw;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDealById: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<DealResponseDto> CreateDeal(DealCreateDto deal)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                INSERT INTO Deals (
                    Title, LocationId, Price, DiscountedPrice, DiscountPercentage,
                    Rating, DaysCount, NightsCount, StartPoint, EndPoint, Duration,
                    Description, Photos, ElderlyFriendly, InternetIncluded,
                    TravelIncluded, MealsIncluded, SightseeingIncluded, StayIncluded,
                    AirTransfer, RoadTransfer, TrainTransfer, TravelCostIncluded,
                    GuideIncluded, PhotographyIncluded, InsuranceIncluded,
                    VisaIncluded, Itinerary, PackageOptions, MapUrl, Policies,
                    PackageType, IsActive, CreatedAt, UpdatedAt, UserId
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @Title, @LocationId, @Price, @DiscountedPrice, @DiscountPercentage,
                    @Rating, @DaysCount, @NightsCount, @StartPoint, @EndPoint, @Duration,
                    @Description, @Photos, @ElderlyFriendly, @InternetIncluded,
                    @TravelIncluded, @MealsIncluded, @SightseeingIncluded, @StayIncluded,
                    @AirTransfer, @RoadTransfer, @TrainTransfer, @TravelCostIncluded,
                    @GuideIncluded, @PhotographyIncluded, @InsuranceIncluded,
                    @VisaIncluded, @Itinerary, @PackageOptions, @MapUrl, @Policies,
                    @PackageType, @IsActive, GETUTCDATE(), GETUTCDATE(), @UserId
                )";

            using var command = new SqlCommand(sql, connection);
            AddDealParameters(command, deal);

            var id = (int)await command.ExecuteScalarAsync();
            return await GetDealById(id);
        }

        public async Task<bool> UpdateDeal(int id, DealUpdateDto deal)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Build SQL dynamically to only update the fields that are provided
            var updateFields = new List<string>();
            var parameters = new List<SqlParameter>();

            // Add parameters only for properties that have values
            if (deal.ViewCount.HasValue)
            {
                updateFields.Add("ViewCount = @ViewCount");
                parameters.Add(new SqlParameter("@ViewCount", deal.ViewCount.Value));
            }

            if (deal.LastViewed.HasValue)
            {
                updateFields.Add("LastViewed = @LastViewed");
                parameters.Add(new SqlParameter("@LastViewed", deal.LastViewed.Value));
            }

            if (deal.ClickCount.HasValue)
            {
                updateFields.Add("ClickCount = @ClickCount");
                parameters.Add(new SqlParameter("@ClickCount", deal.ClickCount.Value));
            }

            if (deal.LastClicked.HasValue)
            {
                updateFields.Add("LastClicked = @LastClicked");
                parameters.Add(new SqlParameter("@LastClicked", deal.LastClicked.Value));
            }

            if (deal.RelevanceScore.HasValue)
            {
                updateFields.Add("RelevanceScore = @RelevanceScore");
                parameters.Add(new SqlParameter("@RelevanceScore", deal.RelevanceScore.Value));
            }

            // Always include IsActive in the update
            updateFields.Add("IsActive = @IsActive");
            parameters.Add(new SqlParameter("@IsActive", deal.IsActive));

            // Only proceed if there are fields to update
            if (updateFields.Count == 0)
            {
                return true; // Nothing to update
            }

            updateFields.Add("UpdatedAt = GETUTCDATE()");

            var sql =
                $@"
                UPDATE Deals
                SET {string.Join(", ", updateFields)}
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddRange(parameters.ToArray());

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteDeal(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                UPDATE Deals
                SET IsActive = 0,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<IEnumerable<DealResponseDto>> SearchDeals(
            string? searchTerm = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minDays = null,
            int? maxDays = null,
            string? packageType = null
        )
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql =
                @"
                SELECT d.*, l.Name as LocationName, l.Description as LocationDescription,
                       l.ImageUrl as LocationImageUrl, l.IsPopular as LocationIsPopular,
                       l.IsActive as LocationIsActive, l.ClickCount as LocationClickCount,
                       l.RequestCallCount as LocationRequestCallCount,
                       l.CreatedAt as LocationCreatedAt, l.UpdatedAt as LocationUpdatedAt
                FROM Deals d
                INNER JOIN Locations l ON d.LocationId = l.Id
                WHERE d.IsActive = 1";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                sql +=
                    @" AND (d.Title LIKE @SearchTerm 
                          OR d.Description LIKE @SearchTerm 
                          OR l.Name LIKE @SearchTerm)";
                parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
            }

            if (minPrice.HasValue)
            {
                sql += " AND d.Price >= @MinPrice";
                parameters.Add(new SqlParameter("@MinPrice", minPrice.Value));
            }

            if (maxPrice.HasValue)
            {
                sql += " AND d.Price <= @MaxPrice";
                parameters.Add(new SqlParameter("@MaxPrice", maxPrice.Value));
            }

            if (minDays.HasValue)
            {
                sql += " AND d.DaysCount >= @MinDays";
                parameters.Add(new SqlParameter("@MinDays", minDays.Value));
            }

            if (maxDays.HasValue)
            {
                sql += " AND d.DaysCount <= @MaxDays";
                parameters.Add(new SqlParameter("@MaxDays", maxDays.Value));
            }

            if (!string.IsNullOrEmpty(packageType))
            {
                sql += " AND d.PackageType = @PackageType";
                parameters.Add(new SqlParameter("@PackageType", packageType));
            }

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var deals = new List<DealResponseDto>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                deals.Add(MapToDealResponseDto(reader));
            }

            return deals;
        }

        public async Task<IEnumerable<DealResponseDto>> GetDealsByUserId(string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT d.Id, d.Title, d.LocationId, d.UserId, d.Price, d.DiscountedPrice, 
                           d.DiscountPercentage, d.Rating, d.DaysCount, d.NightsCount, 
                           d.StartPoint, d.EndPoint, d.Duration, d.Description, d.Photos,
                           d.ElderlyFriendly, d.InternetIncluded, d.TravelIncluded, 
                           d.MealsIncluded, d.SightseeingIncluded, d.StayIncluded,
                           d.AirTransfer, d.RoadTransfer, d.TrainTransfer, 
                           d.TravelCostIncluded, d.GuideIncluded, d.PhotographyIncluded,
                           d.InsuranceIncluded, d.VisaIncluded, d.Itinerary, 
                           d.PackageOptions, d.MapUrl, d.Policies, d.PackageType,
                           d.IsActive, d.CreatedAt, d.UpdatedAt, d.Status,
                           d.ApprovalStatus, d.SearchKeywords,
                           l.Name as LocationName, 
                           l.Description as LocationDescription,
                           l.ImageUrl as LocationImageUrl, 
                           l.IsPopular as LocationIsPopular,
                           l.IsActive as LocationIsActive, 
                           l.ClickCount as LocationClickCount,
                           l.RequestCallCount as LocationRequestCallCount,
                           l.CreatedAt as LocationCreatedAt, 
                           l.UpdatedAt as LocationUpdatedAt,
                           l.Country as LocationCountry,
                           l.Continent as LocationContinent
                    FROM Deals d
                    INNER JOIN Locations l ON d.LocationId = l.Id
                    WHERE d.UserId = @UserId";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var deals = new List<DealResponseDto>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    try
                    {
                        deals.Add(MapToDealResponseDto(reader));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error mapping deal: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        continue;
                    }
                }

                return deals;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDealsByUserId: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> IncrementDealClickCount(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                UPDATE Deals
                SET ClickCount = ClickCount + 1,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ToggleDealStatus(int id, bool isActive)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                UPDATE Deals
                SET IsActive = @IsActive,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IsActive", isActive);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        #endregion

        #region Helper Methods

        private LocationResponseDto MapToLocationResponseDto(SqlDataReader reader)
        {
            return new LocationResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description")),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ImageUrl")),
                Country = reader.IsDBNull(reader.GetOrdinal("Country"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Country")),
                Continent = reader.IsDBNull(reader.GetOrdinal("Continent"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Continent")),
                IsPopular = reader.GetBoolean(reader.GetOrdinal("IsPopular")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                ClickCount = reader.GetInt32(reader.GetOrdinal("ClickCount")),
                RequestCallCount = reader.GetInt32(reader.GetOrdinal("RequestCallCount")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            };
        }

        private DealResponseDto MapToDealResponseDto(SqlDataReader reader)
        {
            return new DealResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                LocationId = reader.GetInt32(reader.GetOrdinal("LocationId")),
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                DiscountedPrice = reader.GetDecimal(reader.GetOrdinal("DiscountedPrice")),
                DiscountPercentage = reader.GetInt32(reader.GetOrdinal("DiscountPercentage")),
                Rating = reader.GetDecimal(reader.GetOrdinal("Rating")),
                DaysCount = reader.GetInt32(reader.GetOrdinal("DaysCount")),
                NightsCount = reader.GetInt32(reader.GetOrdinal("NightsCount")),
                StartPoint = reader.GetString(reader.GetOrdinal("StartPoint")),
                EndPoint = reader.GetString(reader.GetOrdinal("EndPoint")),
                Duration = reader.GetString(reader.GetOrdinal("Duration")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                Photos = JsonSerializer.Deserialize<List<string>>(
                    reader.GetString(reader.GetOrdinal("Photos"))
                ),
                ElderlyFriendly = reader.GetBoolean(reader.GetOrdinal("ElderlyFriendly")),
                InternetIncluded = reader.GetBoolean(reader.GetOrdinal("InternetIncluded")),
                TravelIncluded = reader.GetBoolean(reader.GetOrdinal("TravelIncluded")),
                MealsIncluded = reader.GetBoolean(reader.GetOrdinal("MealsIncluded")),
                SightseeingIncluded = reader.GetBoolean(reader.GetOrdinal("SightseeingIncluded")),
                StayIncluded = reader.GetBoolean(reader.GetOrdinal("StayIncluded")),
                AirTransfer = reader.GetBoolean(reader.GetOrdinal("AirTransfer")),
                RoadTransfer = reader.GetBoolean(reader.GetOrdinal("RoadTransfer")),
                TrainTransfer = reader.GetBoolean(reader.GetOrdinal("TrainTransfer")),
                TravelCostIncluded = reader.GetBoolean(reader.GetOrdinal("TravelCostIncluded")),
                GuideIncluded = reader.GetBoolean(reader.GetOrdinal("GuideIncluded")),
                PhotographyIncluded = reader.GetBoolean(reader.GetOrdinal("PhotographyIncluded")),
                InsuranceIncluded = reader.GetBoolean(reader.GetOrdinal("InsuranceIncluded")),
                VisaIncluded = reader.GetBoolean(reader.GetOrdinal("VisaIncluded")),
                Itinerary = JsonSerializer.Deserialize<List<ItineraryDay>>(
                    reader.GetString(reader.GetOrdinal("Itinerary"))
                ),
                PackageOptions = JsonSerializer.Deserialize<List<PackageOption>>(
                    reader.GetString(reader.GetOrdinal("PackageOptions"))
                ),
                MapUrl = reader.GetString(reader.GetOrdinal("MapUrl")),
                Policies = JsonSerializer.Deserialize<List<Policy>>(
                    reader.GetString(reader.GetOrdinal("Policies"))
                ),
                PackageType = reader.GetString(reader.GetOrdinal("PackageType")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                Status = reader.IsDBNull(reader.GetOrdinal("Status"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Status")),
                ApprovalStatus = reader.IsDBNull(reader.GetOrdinal("ApprovalStatus"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ApprovalStatus")),
                SearchKeywords = reader.IsDBNull(reader.GetOrdinal("SearchKeywords"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("SearchKeywords")),
                Location = new LocationResponseDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("LocationId")),
                    Name = reader.GetString(reader.GetOrdinal("LocationName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("LocationDescription"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("LocationDescription")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("LocationImageUrl"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("LocationImageUrl")),
                    Country = reader.IsDBNull(reader.GetOrdinal("LocationCountry"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("LocationCountry")),
                    Continent = reader.IsDBNull(reader.GetOrdinal("LocationContinent"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("LocationContinent")),
                    IsPopular = reader.GetBoolean(reader.GetOrdinal("LocationIsPopular")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("LocationIsActive")),
                    ClickCount = reader.GetInt32(reader.GetOrdinal("LocationClickCount")),
                    RequestCallCount = reader.GetInt32(
                        reader.GetOrdinal("LocationRequestCallCount")
                    ),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("LocationCreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("LocationUpdatedAt")),
                },
            };
        }

        private void AddDealParameters(SqlCommand command, DealCreateDto deal)
        {
            command.Parameters.AddWithValue("@Title", deal.Title);
            command.Parameters.AddWithValue("@LocationId", deal.LocationId);
            command.Parameters.AddWithValue("@Price", deal.Price);
            command.Parameters.AddWithValue("@DiscountedPrice", deal.DiscountedPrice);
            command.Parameters.AddWithValue("@DiscountPercentage", deal.DiscountPercentage);
            command.Parameters.AddWithValue("@Rating", deal.Rating);
            command.Parameters.AddWithValue("@DaysCount", deal.DaysCount);
            command.Parameters.AddWithValue("@NightsCount", deal.NightsCount);
            command.Parameters.AddWithValue("@StartPoint", deal.StartPoint);
            command.Parameters.AddWithValue("@EndPoint", deal.EndPoint);
            command.Parameters.AddWithValue("@Duration", deal.Duration);
            command.Parameters.AddWithValue("@Description", deal.Description);
            command.Parameters.AddWithValue("@Photos", JsonSerializer.Serialize(deal.Photos));
            command.Parameters.AddWithValue("@ElderlyFriendly", deal.ElderlyFriendly);
            command.Parameters.AddWithValue("@InternetIncluded", deal.InternetIncluded);
            command.Parameters.AddWithValue("@TravelIncluded", deal.TravelIncluded);
            command.Parameters.AddWithValue("@MealsIncluded", deal.MealsIncluded);
            command.Parameters.AddWithValue("@SightseeingIncluded", deal.SightseeingIncluded);
            command.Parameters.AddWithValue("@StayIncluded", deal.StayIncluded);
            command.Parameters.AddWithValue("@AirTransfer", deal.AirTransfer);
            command.Parameters.AddWithValue("@RoadTransfer", deal.RoadTransfer);
            command.Parameters.AddWithValue("@TrainTransfer", deal.TrainTransfer);
            command.Parameters.AddWithValue("@TravelCostIncluded", deal.TravelCostIncluded);
            command.Parameters.AddWithValue("@GuideIncluded", deal.GuideIncluded);
            command.Parameters.AddWithValue("@PhotographyIncluded", deal.PhotographyIncluded);
            command.Parameters.AddWithValue("@InsuranceIncluded", deal.InsuranceIncluded);
            command.Parameters.AddWithValue("@VisaIncluded", deal.VisaIncluded);
            command.Parameters.AddWithValue("@Itinerary", JsonSerializer.Serialize(deal.Itinerary));
            command.Parameters.AddWithValue(
                "@PackageOptions",
                JsonSerializer.Serialize(deal.PackageOptions)
            );
            command.Parameters.AddWithValue("@MapUrl", deal.MapUrl);
            command.Parameters.AddWithValue("@Policies", JsonSerializer.Serialize(deal.Policies));
            command.Parameters.AddWithValue("@PackageType", deal.PackageType);
            command.Parameters.AddWithValue("@IsActive", deal.IsActive);
            command.Parameters.AddWithValue("@UserId", deal.UserId);
        }

        #endregion
    }
}
