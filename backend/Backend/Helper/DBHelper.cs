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
        private readonly ILogger<DBHelper> _logger;

        public DBHelper(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<DBHelper> logger
        )
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
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
                           Country, Continent, Currency
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
                       Country, Continent, Currency
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
                       Country, Continent, Currency
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
                INSERT INTO Locations (Name, Description, ImageUrl, IsPopular, IsActive, CreatedAt, UpdatedAt, ClickCount, RequestCallCount, Currency)
                OUTPUT INSERTED.*
                VALUES (@Name, @Description, @ImageUrl, @IsPopular, @IsActive, GETUTCDATE(), GETUTCDATE(), 0, 0, @Currency)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", location.Name);
            command.Parameters.AddWithValue(
                "@Description",
                (object)location.Description ?? DBNull.Value
            );
            command.Parameters.AddWithValue("@ImageUrl", (object)location.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsPopular", location.IsPopular);
            command.Parameters.AddWithValue("@IsActive", location.IsActive);
            command.Parameters.AddWithValue("@Currency", (object)location.Currency ?? DBNull.Value);

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
                    Currency = @Currency,
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
            command.Parameters.AddWithValue("@Currency", (object)location.Currency ?? DBNull.Value);

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

        public async Task<IEnumerable<DealResponseDto>> GetDeals(
            int? locationId = null,
            string? status = null,
            bool? isActive = null,
            bool? isFeatured = null,
            string? packageType = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minDays = null,
            int? maxDays = null
        )
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql =
                    @"
                    SELECT d.Id, d.Title, d.LocationId, d.UserId, d.Price, d.DiscountedPrice, 
                           d.DiscountPercentage, d.Rating, d.DaysCount, d.NightsCount, 
                           d.Description, d.Photos, d.PackageType, d.IsActive, d.CreatedAt, 
                           d.UpdatedAt, d.Headlines, d.Tags, d.Seasons,
                           l.Name as LocationName, l.Description as LocationDescription,
                           l.ImageUrl as LocationImageUrl, l.IsPopular as LocationIsPopular,
                           l.IsActive as LocationIsActive, l.ClickCount as LocationClickCount,
                           l.RequestCallCount as LocationRequestCallCount,
                           l.CreatedAt as LocationCreatedAt, l.UpdatedAt as LocationUpdatedAt
                    FROM Deals d
                    INNER JOIN Locations l ON d.LocationId = l.Id
                    WHERE 1=1";

                var parameters = new List<SqlParameter>();

                if (locationId.HasValue)
                {
                    sql += " AND d.LocationId = @LocationId";
                    parameters.Add(new SqlParameter("@LocationId", locationId.Value));
                }

                if (isActive.HasValue)
                {
                    sql += " AND d.IsActive = @IsActive";
                    parameters.Add(new SqlParameter("@IsActive", isActive.Value));
                }

                if (isFeatured.HasValue)
                {
                    sql += " AND d.IsFeatured = @IsFeatured";
                    parameters.Add(new SqlParameter("@IsFeatured", isFeatured.Value));
                }

                if (!string.IsNullOrEmpty(packageType))
                {
                    sql += " AND d.PackageType = @PackageType";
                    parameters.Add(new SqlParameter("@PackageType", packageType));
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

                sql += " ORDER BY d.CreatedAt DESC";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddRange(parameters.ToArray());

                var deals = new List<DealResponseDto>();
                using var reader = await command.ExecuteReaderAsync();

                int rowCount = 0;
                while (await reader.ReadAsync())
                {
                    try
                    {
                        // Log raw data for debugging
                        _logger.LogInformation("Processing deal row {RowCount}", rowCount);

                        // Safely handle JSON fields
                        string photosJson = reader.IsDBNull(reader.GetOrdinal("Photos"))
                            ? "[]"
                            : reader.GetString(reader.GetOrdinal("Photos"));
                        string headlinesJson = reader.IsDBNull(reader.GetOrdinal("Headlines"))
                            ? "[]"
                            : reader.GetString(reader.GetOrdinal("Headlines"));
                        string tagsJson = reader.IsDBNull(reader.GetOrdinal("Tags"))
                            ? "[]"
                            : reader.GetString(reader.GetOrdinal("Tags"));
                        string seasonsJson = reader.IsDBNull(reader.GetOrdinal("Seasons"))
                            ? "[]"
                            : reader.GetString(reader.GetOrdinal("Seasons"));

                        var deal = new DealResponseDto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            LocationId = reader.GetInt32(reader.GetOrdinal("LocationId")),
                            UserId = reader.GetString(reader.GetOrdinal("UserId")),
                            Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                            DiscountedPrice = reader.IsDBNull(reader.GetOrdinal("DiscountedPrice"))
                                ? 0
                                : reader.GetDecimal(reader.GetOrdinal("DiscountedPrice")),
                            DiscountPercentage = reader.IsDBNull(
                                reader.GetOrdinal("DiscountPercentage")
                            )
                                ? 0
                                : reader.GetInt32(reader.GetOrdinal("DiscountPercentage")),
                            Rating = reader.IsDBNull(reader.GetOrdinal("Rating"))
                                ? 0
                                : reader.GetDecimal(reader.GetOrdinal("Rating")),
                            DaysCount = reader.GetInt32(reader.GetOrdinal("DaysCount")),
                            NightsCount = reader.GetInt32(reader.GetOrdinal("NightsCount")),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("Description")),
                            Photos = SafeDeserializeJson<List<string>>(photosJson),
                            PackageType = reader.IsDBNull(reader.GetOrdinal("PackageType"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("PackageType")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                            Headlines = reader.IsDBNull(reader.GetOrdinal("Headlines"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Headlines")),
                            Tags = SafeDeserializeJson<List<string>>(tagsJson),
                            Seasons = SafeDeserializeJson<List<string>>(seasonsJson),
                            Location = new LocationResponseDto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("LocationId")),
                                Name = reader.IsDBNull(reader.GetOrdinal("LocationName"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("LocationName")),
                                Description = reader.IsDBNull(
                                    reader.GetOrdinal("LocationDescription")
                                )
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("LocationDescription")),
                                ImageUrl = reader.IsDBNull(reader.GetOrdinal("LocationImageUrl"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("LocationImageUrl")),
                                IsPopular = reader.GetBoolean(
                                    reader.GetOrdinal("LocationIsPopular")
                                ),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("LocationIsActive")),
                                ClickCount = reader.GetInt32(
                                    reader.GetOrdinal("LocationClickCount")
                                ),
                                RequestCallCount = reader.GetInt32(
                                    reader.GetOrdinal("LocationRequestCallCount")
                                ),
                                CreatedAt = reader.GetDateTime(
                                    reader.GetOrdinal("LocationCreatedAt")
                                ),
                                UpdatedAt = reader.GetDateTime(
                                    reader.GetOrdinal("LocationUpdatedAt")
                                ),
                            },
                        };
                        deals.Add(deal);
                        rowCount++;
                        _logger.LogInformation("Successfully mapped deal {DealId}", deal.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error mapping deal row {RowCount}. Raw data: {RawData}",
                            rowCount,
                            $"Id: {reader.GetInt32(reader.GetOrdinal("Id"))}, "
                                + $"Title: {reader.GetString(reader.GetOrdinal("Title"))}, "
                                + $"Photos: {(reader.IsDBNull(reader.GetOrdinal("Photos")) ? "NULL" : reader.GetString(reader.GetOrdinal("Photos")))}"
                        );
                        continue;
                    }
                }

                _logger.LogInformation("Query returned {RowCount} deals", rowCount);
                return deals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deals");
                throw;
            }
        }

        private T SafeDeserializeJson<T>(string json)
            where T : class, new()
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                    return new T();

                // If the string doesn't start with [ or {, it's not valid JSON
                if (!json.TrimStart().StartsWith("[") && !json.TrimStart().StartsWith("{"))
                {
                    _logger.LogWarning("Invalid JSON format: {Json}", json);
                    return new T();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                };

                return JsonSerializer.Deserialize<T>(json, options) ?? new T();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing JSON: {Json}", json);
                return new T();
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
                           d.Description, d.Photos,
                           d.ElderlyFriendly, d.InternetIncluded, d.TravelIncluded, 
                           d.MealsIncluded, d.SightseeingIncluded, d.StayIncluded,
                           d.AirTransfer, d.RoadTransfer, d.TrainTransfer, 
                           d.TravelCostIncluded, d.GuideIncluded, d.PhotographyIncluded,
                           d.InsuranceIncluded, d.VisaIncluded, d.Itinerary, 
                           d.PackageOptions, d.MapUrl, d.Policies, d.PackageType,
                           d.IsActive, d.CreatedAt, d.UpdatedAt,
                           d.SearchKeywords, d.ClickCount, d.ViewCount, d.BookingCount,
                           d.LastClicked, d.LastViewed, d.LastBooked, d.RelevanceScore,
                           d.IsFeatured, d.FeaturedUntil, d.Priority, d.Headlines, d.Tags,
                           d.Seasons, d.DifficultyLevel, d.MaxGroupSize, d.MinGroupSize,
                           d.IsInstantBooking, d.IsLastMinuteDeal, d.ValidFrom, d.ValidUntil,
                           d.CancellationPolicy, d.RefundPolicy, d.Languages,
                           d.Requirements, d.Restrictions,
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
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    INSERT INTO Deals (
                        Title, LocationId, UserId, Price, DiscountedPrice, DiscountPercentage,
                        Rating, DaysCount, NightsCount, Description, Photos, ElderlyFriendly,
                        InternetIncluded, TravelIncluded, MealsIncluded, SightseeingIncluded,
                        StayIncluded, AirTransfer, RoadTransfer, TrainTransfer, TravelCostIncluded,
                        GuideIncluded, PhotographyIncluded, InsuranceIncluded, VisaIncluded,
                        Itinerary, PackageOptions, MapUrl, Policies, PackageType, IsActive,
                        CreatedAt, UpdatedAt, Headlines, Tags, Seasons, DifficultyLevel,
                        MaxGroupSize, MinGroupSize, IsInstantBooking, IsLastMinuteDeal,
                        ValidFrom, ValidUntil, CancellationPolicy, RefundPolicy, Languages,
                        Requirements, Restrictions
                    )
                    OUTPUT INSERTED.*
                    VALUES (
                        @Title, @LocationId, @UserId, @Price, @DiscountedPrice, @DiscountPercentage,
                        @Rating, @DaysCount, @NightsCount, @Description, @Photos, @ElderlyFriendly,
                        @InternetIncluded, @TravelIncluded, @MealsIncluded, @SightseeingIncluded,
                        @StayIncluded, @AirTransfer, @RoadTransfer, @TrainTransfer, @TravelCostIncluded,
                        @GuideIncluded, @PhotographyIncluded, @InsuranceIncluded, @VisaIncluded,
                        @Itinerary, @PackageOptions, @MapUrl, @Policies, @PackageType, @IsActive,
                        @CreatedAt, @UpdatedAt, @Headlines, @Tags, @Seasons, @DifficultyLevel,
                        @MaxGroupSize, @MinGroupSize, @IsInstantBooking, @IsLastMinuteDeal,
                        @ValidFrom, @ValidUntil, @CancellationPolicy, @RefundPolicy, @Languages,
                        @Requirements, @Restrictions
                    )";

                using var command = new SqlCommand(sql, connection);
                AddDealParameters(command, deal);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToDealResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deal");
                throw;
            }
        }

        public async Task<bool> UpdateDeal(int id, DealUpdateDto deal)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var updateFields = new List<string>();
                var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

                // Add parameters only for properties that have values
                if (!string.IsNullOrEmpty(deal.Title))
                {
                    updateFields.Add("Title = @Title");
                    parameters.Add(new SqlParameter("@Title", deal.Title));
                }

                if (deal.LocationId.HasValue)
                {
                    updateFields.Add("LocationId = @LocationId");
                    parameters.Add(new SqlParameter("@LocationId", deal.LocationId.Value));
                }

                if (deal.Price.HasValue)
                {
                    updateFields.Add("Price = @Price");
                    parameters.Add(new SqlParameter("@Price", deal.Price.Value));
                }

                if (deal.DiscountedPrice.HasValue)
                {
                    updateFields.Add("DiscountedPrice = @DiscountedPrice");
                    parameters.Add(
                        new SqlParameter("@DiscountedPrice", deal.DiscountedPrice.Value)
                    );
                }

                if (deal.DiscountPercentage.HasValue)
                {
                    updateFields.Add("DiscountPercentage = @DiscountPercentage");
                    parameters.Add(
                        new SqlParameter("@DiscountPercentage", deal.DiscountPercentage.Value)
                    );
                }

                if (deal.Rating.HasValue)
                {
                    updateFields.Add("Rating = @Rating");
                    parameters.Add(new SqlParameter("@Rating", deal.Rating.Value));
                }

                if (deal.DaysCount.HasValue)
                {
                    updateFields.Add("DaysCount = @DaysCount");
                    parameters.Add(new SqlParameter("@DaysCount", deal.DaysCount.Value));
                }

                if (deal.NightsCount.HasValue)
                {
                    updateFields.Add("NightsCount = @NightsCount");
                    parameters.Add(new SqlParameter("@NightsCount", deal.NightsCount.Value));
                }

                if (!string.IsNullOrEmpty(deal.Description))
                {
                    updateFields.Add("Description = @Description");
                    parameters.Add(new SqlParameter("@Description", deal.Description));
                }

                if (deal.Photos != null)
                {
                    updateFields.Add("Photos = @Photos");
                    parameters.Add(
                        new SqlParameter("@Photos", JsonSerializer.Serialize(deal.Photos))
                    );
                }

                // Add all boolean flags
                if (deal.ElderlyFriendly.HasValue)
                {
                    updateFields.Add("ElderlyFriendly = @ElderlyFriendly");
                    parameters.Add(
                        new SqlParameter("@ElderlyFriendly", deal.ElderlyFriendly.Value)
                    );
                }

                if (deal.InternetIncluded.HasValue)
                {
                    updateFields.Add("InternetIncluded = @InternetIncluded");
                    parameters.Add(
                        new SqlParameter("@InternetIncluded", deal.InternetIncluded.Value)
                    );
                }

                if (deal.TravelIncluded.HasValue)
                {
                    updateFields.Add("TravelIncluded = @TravelIncluded");
                    parameters.Add(new SqlParameter("@TravelIncluded", deal.TravelIncluded.Value));
                }

                if (deal.MealsIncluded.HasValue)
                {
                    updateFields.Add("MealsIncluded = @MealsIncluded");
                    parameters.Add(new SqlParameter("@MealsIncluded", deal.MealsIncluded.Value));
                }

                if (deal.SightseeingIncluded.HasValue)
                {
                    updateFields.Add("SightseeingIncluded = @SightseeingIncluded");
                    parameters.Add(
                        new SqlParameter("@SightseeingIncluded", deal.SightseeingIncluded.Value)
                    );
                }

                if (deal.StayIncluded.HasValue)
                {
                    updateFields.Add("StayIncluded = @StayIncluded");
                    parameters.Add(new SqlParameter("@StayIncluded", deal.StayIncluded.Value));
                }

                if (deal.AirTransfer.HasValue)
                {
                    updateFields.Add("AirTransfer = @AirTransfer");
                    parameters.Add(new SqlParameter("@AirTransfer", deal.AirTransfer.Value));
                }

                if (deal.RoadTransfer.HasValue)
                {
                    updateFields.Add("RoadTransfer = @RoadTransfer");
                    parameters.Add(new SqlParameter("@RoadTransfer", deal.RoadTransfer.Value));
                }

                if (deal.TrainTransfer.HasValue)
                {
                    updateFields.Add("TrainTransfer = @TrainTransfer");
                    parameters.Add(new SqlParameter("@TrainTransfer", deal.TrainTransfer.Value));
                }

                if (deal.TravelCostIncluded.HasValue)
                {
                    updateFields.Add("TravelCostIncluded = @TravelCostIncluded");
                    parameters.Add(
                        new SqlParameter("@TravelCostIncluded", deal.TravelCostIncluded.Value)
                    );
                }

                if (deal.GuideIncluded.HasValue)
                {
                    updateFields.Add("GuideIncluded = @GuideIncluded");
                    parameters.Add(new SqlParameter("@GuideIncluded", deal.GuideIncluded.Value));
                }

                if (deal.PhotographyIncluded.HasValue)
                {
                    updateFields.Add("PhotographyIncluded = @PhotographyIncluded");
                    parameters.Add(
                        new SqlParameter("@PhotographyIncluded", deal.PhotographyIncluded.Value)
                    );
                }

                if (deal.InsuranceIncluded.HasValue)
                {
                    updateFields.Add("InsuranceIncluded = @InsuranceIncluded");
                    parameters.Add(
                        new SqlParameter("@InsuranceIncluded", deal.InsuranceIncluded.Value)
                    );
                }

                if (deal.VisaIncluded.HasValue)
                {
                    updateFields.Add("VisaIncluded = @VisaIncluded");
                    parameters.Add(new SqlParameter("@VisaIncluded", deal.VisaIncluded.Value));
                }

                if (deal.Itinerary != null)
                {
                    updateFields.Add("Itinerary = @Itinerary");
                    parameters.Add(
                        new SqlParameter("@Itinerary", JsonSerializer.Serialize(deal.Itinerary))
                    );
                }

                if (deal.PackageOptions != null)
                {
                    updateFields.Add("PackageOptions = @PackageOptions");
                    parameters.Add(
                        new SqlParameter(
                            "@PackageOptions",
                            JsonSerializer.Serialize(deal.PackageOptions)
                        )
                    );
                }

                if (!string.IsNullOrEmpty(deal.MapUrl))
                {
                    updateFields.Add("MapUrl = @MapUrl");
                    parameters.Add(new SqlParameter("@MapUrl", deal.MapUrl));
                }

                if (deal.Policies != null)
                {
                    updateFields.Add("Policies = @Policies");
                    parameters.Add(
                        new SqlParameter("@Policies", JsonSerializer.Serialize(deal.Policies))
                    );
                }

                if (!string.IsNullOrEmpty(deal.PackageType))
                {
                    updateFields.Add("PackageType = @PackageType");
                    parameters.Add(new SqlParameter("@PackageType", deal.PackageType));
                }

                if (deal.IsActive.HasValue)
                {
                    updateFields.Add("IsActive = @IsActive");
                    parameters.Add(new SqlParameter("@IsActive", deal.IsActive.Value));
                }

                if (deal.Headlines != null)
                {
                    updateFields.Add("Headlines = @Headlines");
                    parameters.Add(new SqlParameter("@Headlines", deal.Headlines));
                }

                if (deal.Tags != null)
                {
                    updateFields.Add("Tags = @Tags");
                    parameters.Add(new SqlParameter("@Tags", JsonSerializer.Serialize(deal.Tags)));
                }

                if (deal.Seasons != null)
                {
                    updateFields.Add("Seasons = @Seasons");
                    parameters.Add(
                        new SqlParameter("@Seasons", JsonSerializer.Serialize(deal.Seasons))
                    );
                }

                if (!string.IsNullOrEmpty(deal.DifficultyLevel))
                {
                    updateFields.Add("DifficultyLevel = @DifficultyLevel");
                    parameters.Add(new SqlParameter("@DifficultyLevel", deal.DifficultyLevel));
                }

                if (deal.MaxGroupSize.HasValue)
                {
                    updateFields.Add("MaxGroupSize = @MaxGroupSize");
                    parameters.Add(new SqlParameter("@MaxGroupSize", deal.MaxGroupSize.Value));
                }

                if (deal.MinGroupSize.HasValue)
                {
                    updateFields.Add("MinGroupSize = @MinGroupSize");
                    parameters.Add(new SqlParameter("@MinGroupSize", deal.MinGroupSize.Value));
                }

                if (deal.IsInstantBooking.HasValue)
                {
                    updateFields.Add("IsInstantBooking = @IsInstantBooking");
                    parameters.Add(
                        new SqlParameter("@IsInstantBooking", deal.IsInstantBooking.Value)
                    );
                }

                if (deal.IsLastMinuteDeal.HasValue)
                {
                    updateFields.Add("IsLastMinuteDeal = @IsLastMinuteDeal");
                    parameters.Add(
                        new SqlParameter("@IsLastMinuteDeal", deal.IsLastMinuteDeal.Value)
                    );
                }

                if (deal.ValidFrom.HasValue)
                {
                    updateFields.Add("ValidFrom = @ValidFrom");
                    parameters.Add(new SqlParameter("@ValidFrom", deal.ValidFrom.Value));
                }

                if (deal.ValidUntil.HasValue)
                {
                    updateFields.Add("ValidUntil = @ValidUntil");
                    parameters.Add(new SqlParameter("@ValidUntil", deal.ValidUntil.Value));
                }

                if (!string.IsNullOrEmpty(deal.CancellationPolicy))
                {
                    updateFields.Add("CancellationPolicy = @CancellationPolicy");
                    parameters.Add(
                        new SqlParameter("@CancellationPolicy", deal.CancellationPolicy)
                    );
                }

                if (!string.IsNullOrEmpty(deal.RefundPolicy))
                {
                    updateFields.Add("RefundPolicy = @RefundPolicy");
                    parameters.Add(new SqlParameter("@RefundPolicy", deal.RefundPolicy));
                }

                if (deal.Languages != null)
                {
                    updateFields.Add("Languages = @Languages");
                    parameters.Add(
                        new SqlParameter("@Languages", JsonSerializer.Serialize(deal.Languages))
                    );
                }

                if (deal.Requirements != null)
                {
                    updateFields.Add("Requirements = @Requirements");
                    parameters.Add(
                        new SqlParameter(
                            "@Requirements",
                            JsonSerializer.Serialize(deal.Requirements)
                        )
                    );
                }

                if (deal.Restrictions != null)
                {
                    updateFields.Add("Restrictions = @Restrictions");
                    parameters.Add(
                        new SqlParameter(
                            "@Restrictions",
                            JsonSerializer.Serialize(deal.Restrictions)
                        )
                    );
                }

                // Always update the UpdatedAt timestamp
                updateFields.Add("UpdatedAt = @UpdatedAt");
                parameters.Add(new SqlParameter("@UpdatedAt", deal.UpdatedAt));

                if (updateFields.Count == 0)
                {
                    return true; // Nothing to update
                }

                var sql =
                    $@"
                    UPDATE Deals
                    SET {string.Join(", ", updateFields)}
                    WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddRange(parameters.ToArray());

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deal {DealId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteDeal(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                DELETE FROM Deals
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
            string? packageType = null,
            string? difficultyLevel = null,
            bool? isInstantBooking = null,
            bool? isLastMinuteDeal = null,
            string? status = null
        )
        {
            try
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
                        @" AND (
                        d.Title LIKE @SearchTerm 
                        OR d.Description LIKE @SearchTerm 
                        OR d.SearchKeywords LIKE @SearchTerm
                        OR l.Name LIKE @SearchTerm
                    )";
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

                if (!string.IsNullOrEmpty(difficultyLevel))
                {
                    sql += " AND d.DifficultyLevel = @DifficultyLevel";
                    parameters.Add(new SqlParameter("@DifficultyLevel", difficultyLevel));
                }

                if (isInstantBooking.HasValue)
                {
                    sql += " AND d.IsInstantBooking = @IsInstantBooking";
                    parameters.Add(new SqlParameter("@IsInstantBooking", isInstantBooking.Value));
                }

                if (isLastMinuteDeal.HasValue)
                {
                    sql += " AND d.IsLastMinuteDeal = @IsLastMinuteDeal";
                    parameters.Add(new SqlParameter("@IsLastMinuteDeal", isLastMinuteDeal.Value));
                }

                sql += " ORDER BY d.RelevanceScore DESC, d.Priority DESC, d.CreatedAt DESC";

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching deals");
                throw;
            }
        }

        public async Task<IEnumerable<DealResponseDto>> GetDealsByUserId(
            string userId,
            string? status = null,
            bool? isActive = null
        )
        {
            try
            {
                _logger.LogInformation("Getting deals for user ID: {UserId}", userId);

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
                    WHERE d.UserId = @UserId";

                var parameters = new List<SqlParameter> { new SqlParameter("@UserId", userId) };

                if (isActive.HasValue)
                {
                    sql += " AND d.IsActive = @IsActive";
                    parameters.Add(new SqlParameter("@IsActive", isActive.Value));
                }

                sql += " ORDER BY d.CreatedAt DESC";

                _logger.LogDebug("Executing SQL query: {Sql}", sql);

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddRange(parameters.ToArray());

                var deals = new List<DealResponseDto>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    try
                    {
                        var deal = MapToDealResponseDto(reader);
                        deals.Add(deal);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error mapping deal for user {UserId}. Skipping this deal. Exception: {Exception}",
                            userId,
                            ex.Message
                        );
                        // Skip this deal and continue with others
                    }
                }

                _logger.LogInformation(
                    "Retrieved {Count} deals for user {UserId}",
                    deals.Count,
                    userId
                );
                return deals;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting deals for user {UserId}. Exception: {Exception}",
                    userId,
                    ex.Message
                );
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

        #region Booking Operations

        public async Task<BookingResponseDTO> CreateBooking(CreateBookingDTO model, string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // First check if user already has a booking for this deal
                const string checkSql =
                    @"
                    SELECT COUNT(*) 
                    FROM Bookings 
                    WHERE UserId = @UserId AND DealId = @DealId";

                using var checkCommand = new SqlCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@UserId", userId);
                checkCommand.Parameters.AddWithValue("@DealId", model.DealId);

                var existingCount = (int)await checkCommand.ExecuteScalarAsync();
                if (existingCount > 0)
                {
                    throw new Exception("You have already made an inquiry for this deal.");
                }

                const string sql =
                    @"
                    DECLARE @InsertedId TABLE (Id INT);
                    
                    INSERT INTO Bookings (
                        UserId, AgencyId, DealId, Status, CreatedAt, UpdatedAt,
                        NumberOfPeople, TravelDate, SpecialRequirements,
                        Email, PhoneNumber, BookingMessage,
                        HasUnreadMessages, LastMessage, LastMessageAt, LastMessageBy
                    )
                    OUTPUT INSERTED.Id INTO @InsertedId
                    VALUES (
                        @UserId, @AgencyId, @DealId, 'Pending', GETUTCDATE(), GETUTCDATE(),
                        @NumberOfPeople, @TravelDate, @SpecialRequirements,
                        @Email, @PhoneNumber, @BookingMessage,
                        0, NULL, NULL, NULL
                    );

                    SELECT b.*, u.FullName as UserFullName, a.AgencyName
                    FROM Bookings b
                    INNER JOIN AspNetUsers u ON b.UserId = u.Id
                    INNER JOIN AgencyApplications a ON b.AgencyId = a.UserId
                    WHERE b.Id = (SELECT TOP 1 Id FROM @InsertedId);";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@AgencyId", model.AgencyId);
                command.Parameters.AddWithValue("@DealId", model.DealId);
                command.Parameters.AddWithValue("@NumberOfPeople", model.NumberOfPeople);
                command.Parameters.AddWithValue("@Email", model.Email);
                command.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                command.Parameters.AddWithValue("@BookingMessage", model.BookingMessage);
                command.Parameters.AddWithValue(
                    "@TravelDate",
                    model.TravelDate ?? (object)DBNull.Value
                );
                command.Parameters.AddWithValue(
                    "@SpecialRequirements",
                    model.SpecialRequirements ?? (object)DBNull.Value
                );

                _logger.LogInformation(
                    "Creating booking with parameters: {@Parameters}",
                    new
                    {
                        UserId = userId,
                        model.AgencyId,
                        model.DealId,
                        model.NumberOfPeople,
                        model.Email,
                        model.PhoneNumber,
                        model.BookingMessage,
                        model.TravelDate,
                        model.SpecialRequirements,
                    }
                );

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToBookingResponseDto(reader);
                }

                _logger.LogError("Failed to create booking - no rows returned");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<BookingResponseDTO>> GetBookings(string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT 
                        b.*,
                        u.FullName as UserFullName,
                        a.FullName as AgencyName,
                        d.Title as DealName
                    FROM Bookings b
                    INNER JOIN AspNetUsers u ON b.UserId = u.Id
                    INNER JOIN AspNetUsers a ON b.AgencyId = a.Id
                    INNER JOIN Deals d ON b.DealId = d.Id
                    WHERE b.AgencyId = @UserId
                    ORDER BY b.CreatedAt DESC";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var bookings = new List<BookingResponseDTO>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    bookings.Add(MapToBookingResponseDto(reader));
                }

                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<BookingResponseDTO> GetBookingById(int id, string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT b.*, u.FullName as UserFullName, a.FullName as AgencyName
                    FROM Bookings b
                    INNER JOIN AspNetUsers u ON b.UserId = u.Id
                    INNER JOIN AspNetUsers a ON b.AgencyId = a.Id
                    WHERE b.Id = @Id AND (b.UserId = @UserId OR b.AgencyId = @UserId)";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToBookingResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking");
                throw;
            }
        }

        public async Task<bool> UpdateBookingStatus(
            int id,
            string status,
            string reason,
            string userId
        )
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql =
                    @"
                    UPDATE Bookings
                    SET Status = @Status,
                        UpdatedAt = GETUTCDATE(),";

                switch (status.ToLower())
                {
                    case "accepted":
                        sql += " AcceptedAt = GETUTCDATE()";
                        break;
                    case "rejected":
                        sql += " RejectedAt = GETUTCDATE(), RejectionReason = @Reason";
                        break;
                    case "cancelled":
                        sql += " CancelledAt = GETUTCDATE(), CancellationReason = @Reason";
                        break;
                    case "completed":
                        sql += " CompletedAt = GETUTCDATE()";
                        break;
                }

                sql += " WHERE Id = @Id AND (UserId = @UserId OR AgencyId = @UserId)";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Reason", (object)reason ?? DBNull.Value);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booking status");
                throw;
            }
        }

        public async Task<bool> IncrementBookingCount(int dealId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    UPDATE Deals
                    SET BookingCount = BookingCount + 1,
                        LastBooked = GETUTCDATE(),
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @DealId";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@DealId", dealId);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing booking count");
                throw;
            }
        }

        private BookingResponseDTO MapToBookingResponseDto(SqlDataReader reader)
        {
            try
            {
                return new BookingResponseDTO
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    UserName = reader.IsDBNull(reader.GetOrdinal("UserFullName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("UserFullName")),
                    AgencyId = reader.GetString(reader.GetOrdinal("AgencyId")),
                    AgencyName = reader.IsDBNull(reader.GetOrdinal("AgencyName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("AgencyName")),
                    DealId = reader.GetInt32(reader.GetOrdinal("DealId")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    NumberOfPeople = reader.GetInt32(reader.GetOrdinal("NumberOfPeople")),
                    TravelDate = reader.IsDBNull(reader.GetOrdinal("TravelDate"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("TravelDate")),
                    SpecialRequirements = reader.IsDBNull(reader.GetOrdinal("SpecialRequirements"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("SpecialRequirements")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Notes")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                    BookingMessage = reader.GetString(reader.GetOrdinal("BookingMessage")),
                    LastMessage = reader.IsDBNull(reader.GetOrdinal("LastMessage"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("LastMessage")),
                    LastMessageAt = reader.IsDBNull(reader.GetOrdinal("LastMessageAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("LastMessageAt")),
                    HasUnreadMessages = reader.GetBoolean(reader.GetOrdinal("HasUnreadMessages")),
                    TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount"))
                        ? null
                        : reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                    PaymentStatus = reader.IsDBNull(reader.GetOrdinal("PaymentStatus"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("PaymentStatus")),
                    LastMessageBy = reader.IsDBNull(reader.GetOrdinal("LastMessageBy"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("LastMessageBy")),
                    AcceptedAt = reader.IsDBNull(reader.GetOrdinal("AcceptedAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("AcceptedAt")),
                    RejectedAt = reader.IsDBNull(reader.GetOrdinal("RejectedAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("RejectedAt")),
                    CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                    CancelledAt = reader.IsDBNull(reader.GetOrdinal("CancelledAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("CancelledAt")),
                    RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("RejectionReason")),
                    CancellationReason = reader.IsDBNull(reader.GetOrdinal("CancellationReason"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("CancellationReason")),
                    PaymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("PaymentMethod")),
                    PaymentId = reader.IsDBNull(reader.GetOrdinal("PaymentId"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("PaymentId")),
                    PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                    Rating = reader.IsDBNull(reader.GetOrdinal("Rating"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("Rating")),
                    Review = reader.IsDBNull(reader.GetOrdinal("Review"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Review")),
                    ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping booking response: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> UpdateBookingLastMessage(
            int bookingId,
            string message,
            string userId
        )
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    UPDATE Bookings 
                    SET LastMessage = @Message,
                        LastMessageAt = GETUTCDATE(),
                        LastMessageBy = @UserId,
                        HasUnreadMessages = 1
                    WHERE Id = @BookingId";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BookingId", bookingId);
                command.Parameters.AddWithValue("@Message", message);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booking last message");
                throw;
            }
        }

        public async Task<IEnumerable<UserBookingResponseDTO>> GetUserBookings(string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT 
                        b.Id,
                        b.AgencyId,
                        a.FullName as AgencyName,
                        b.DealId,
                        b.Status,
                        b.CreatedAt,
                        b.UpdatedAt,
                        b.NumberOfPeople,
                        b.TravelDate,
                        b.SpecialRequirements,
                        b.LastMessage,
                        b.LastMessageAt,
                        b.HasUnreadMessages,
                        b.TotalAmount,
                        b.PaymentStatus
                    FROM Bookings b
                    INNER JOIN AspNetUsers a ON b.AgencyId = a.Id
                    WHERE b.UserId = @UserId
                    ORDER BY b.CreatedAt DESC";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var bookings = new List<UserBookingResponseDTO>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    bookings.Add(
                        new UserBookingResponseDTO
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            AgencyId = reader.GetString(reader.GetOrdinal("AgencyId")),
                            AgencyName = reader.GetString(reader.GetOrdinal("AgencyName")),
                            DealId = reader.GetInt32(reader.GetOrdinal("DealId")),
                            Status = reader.GetString(reader.GetOrdinal("Status")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                            NumberOfPeople = reader.GetInt32(reader.GetOrdinal("NumberOfPeople")),
                            TravelDate = reader.IsDBNull(reader.GetOrdinal("TravelDate"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("TravelDate")),
                            SpecialRequirements = reader.IsDBNull(
                                reader.GetOrdinal("SpecialRequirements")
                            )
                                ? null
                                : reader.GetString(reader.GetOrdinal("SpecialRequirements")),
                            LastMessage = reader.IsDBNull(reader.GetOrdinal("LastMessage"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("LastMessage")),
                            LastMessageAt = reader.IsDBNull(reader.GetOrdinal("LastMessageAt"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("LastMessageAt")),
                            HasUnreadMessages = reader.GetBoolean(
                                reader.GetOrdinal("HasUnreadMessages")
                            ),
                            TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount"))
                                ? null
                                : reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                            PaymentStatus = reader.IsDBNull(reader.GetOrdinal("PaymentStatus"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("PaymentStatus")),
                        }
                    );
                }

                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user bookings");
                throw;
            }
        }

        #endregion

        #region Chat Message Operations

        public async Task<ChatMessageResponseDTO> SendMessage(
            int bookingId,
            SendMessageDTO model,
            string senderId
        )
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    DECLARE @InsertedId TABLE (Id INT);
                    
                    INSERT INTO ChatMessages (
                        BookingId, SenderId, ReceiverId, Message, SentAt,
                        MessageType, FileUrl, FileName, FileSize, IsRead, IsDeleted
                    )
                    OUTPUT INSERTED.Id INTO @InsertedId
                    VALUES (
                        @BookingId, @SenderId, @ReceiverId, @Message, GETUTCDATE(),
                        @MessageType, @FileUrl, @FileName, @FileSize, 0, 0
                    );

                    SELECT m.*, u.FullName as SenderName
                    FROM ChatMessages m
                    INNER JOIN AspNetUsers u ON m.SenderId = u.Id
                    WHERE m.Id = (SELECT TOP 1 Id FROM @InsertedId);

                    UPDATE Bookings
                    SET LastMessage = @Message,
                        LastMessageAt = GETUTCDATE(),
                        HasUnreadMessages = 1,
                        LastMessageBy = @SenderId,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @BookingId;";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BookingId", bookingId);
                command.Parameters.AddWithValue("@SenderId", senderId);
                command.Parameters.AddWithValue("@ReceiverId", model.ReceiverId);
                command.Parameters.AddWithValue("@Message", model.Message);
                command.Parameters.AddWithValue(
                    "@MessageType",
                    (object)model.MessageType ?? DBNull.Value
                );
                command.Parameters.AddWithValue("@FileUrl", (object)model.FileUrl ?? DBNull.Value);
                command.Parameters.AddWithValue(
                    "@FileName",
                    (object)model.FileName ?? DBNull.Value
                );
                command.Parameters.AddWithValue(
                    "@FileSize",
                    (object)model.FileSize ?? DBNull.Value
                );

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToChatMessageResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                throw;
            }
        }

        public async Task<IEnumerable<ChatMessageResponseDTO>> GetChatMessages(
            int bookingId,
            string userId
        )
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT m.*, u.FullName as SenderName
                    FROM ChatMessages m
                    INNER JOIN AspNetUsers u ON m.SenderId = u.Id
                    WHERE m.BookingId = @BookingId
                    ORDER BY m.SentAt ASC";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BookingId", bookingId);

                var messages = new List<ChatMessageResponseDTO>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    messages.Add(MapToChatMessageResponseDto(reader));
                }

                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat messages");
                throw;
            }
        }

        public async Task<bool> MarkMessageAsRead(int messageId, string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    UPDATE ChatMessages
                    SET IsRead = 1,
                        ReadAt = GETUTCDATE()
                    WHERE Id = @MessageId AND ReceiverId = @UserId;

                    UPDATE b
                    SET b.HasUnreadMessages = 
                        CASE 
                            WHEN EXISTS (
                                SELECT 1 FROM ChatMessages m 
                                WHERE m.BookingId = b.Id 
                                AND m.ReceiverId = @UserId 
                                AND m.IsRead = 0
                            ) THEN 1
                            ELSE 0
                        END
                    FROM Bookings b
                    INNER JOIN ChatMessages m ON b.Id = m.BookingId
                    WHERE m.Id = @MessageId;";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@MessageId", messageId);
                command.Parameters.AddWithValue("@UserId", userId);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
                throw;
            }
        }

        private ChatMessageResponseDTO MapToChatMessageResponseDto(SqlDataReader reader)
        {
            try
            {
                return new ChatMessageResponseDTO
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    BookingId = reader.GetInt32(reader.GetOrdinal("BookingId")),
                    SenderId = reader.GetString(reader.GetOrdinal("SenderId")),
                    SenderName = reader.IsDBNull(reader.GetOrdinal("SenderName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("SenderName")),
                    ReceiverId = reader.GetString(reader.GetOrdinal("ReceiverId")),
                    Message = reader.GetString(reader.GetOrdinal("Message")),
                    SentAt = reader.GetDateTime(reader.GetOrdinal("SentAt")),
                    ReadAt = reader.IsDBNull(reader.GetOrdinal("ReadAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ReadAt")),
                    IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                    MessageType = reader.IsDBNull(reader.GetOrdinal("MessageType"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("MessageType")),
                    FileUrl = reader.IsDBNull(reader.GetOrdinal("FileUrl"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("FileUrl")),
                    FileName = reader.IsDBNull(reader.GetOrdinal("FileName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("FileName")),
                    FileSize = reader.IsDBNull(reader.GetOrdinal("FileSize"))
                        ? null
                        : reader.GetInt64(reader.GetOrdinal("FileSize")),
                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping chat message response: {Message}", ex.Message);
                throw;
            }
        }

        #endregion

        #region Agency Profile Operations

        public async Task<AgencyProfileResponseDTO> CreateAgencyProfile(
            CreateAgencyProfileDTO model,
            string userId
        )
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // First, get the agency application ID for the user
                    var applicationId = await GetAgencyApplicationId(userId);
                    if (applicationId == 0)
                    {
                        throw new Exception("No agency application found for the user");
                    }

                    // Convert lists to JSON strings
                    var languagesJson = JsonSerializer.Serialize(
                        model.Languages ?? new List<string>()
                    );
                    var socialMediaLinksJson = JsonSerializer.Serialize(
                        model.SocialMediaLinks ?? new List<SocialMediaLinkDTO>()
                    );
                    var teamMembersJson = JsonSerializer.Serialize(
                        model.TeamMembers ?? new List<TeamMemberDTO>()
                    );
                    var certificationsJson = JsonSerializer.Serialize(
                        model.Certifications ?? new List<CertificationDTO>()
                    );
                    var awardsJson = JsonSerializer.Serialize(model.Awards ?? new List<AwardDTO>());
                    var testimonialsJson = JsonSerializer.Serialize(
                        model.Testimonials ?? new List<TestimonialDTO>()
                    );

                    var command = new SqlCommand(
                        @"
                    INSERT INTO AgencyProfiles (
                            UserId,
                            AgencyApplicationId,
                            Website,
                            Email,
                            LogoUrl,
                            CoverImageUrl,
                            OfficeHours,
                            Languages,
                            Specializations,
                            SocialMediaLinks,
                            TeamMembers,
                            Certifications,
                            Awards,
                            Testimonials,
                            TermsAndConditions,
                            PrivacyPolicy,
                            Rating,
                            TotalReviews,
                            TotalBookings,
                            TotalDeals,
                            LastActive,
                            IsOnline,
                            CreatedAt,
                            UpdatedAt
                        ) VALUES (
                            @UserId,
                            @AgencyApplicationId,
                            @Website,
                            @Email,
                            @LogoUrl,
                            @CoverImageUrl,
                            @OfficeHours,
                            @Languages,
                            @Specializations,
                            @SocialMediaLinks,
                            @TeamMembers,
                            @Certifications,
                            @Awards,
                            @Testimonials,
                            @TermsAndConditions,
                            @PrivacyPolicy,
                            @Rating,
                            @TotalReviews,
                            @TotalBookings,
                            @TotalDeals,
                            @LastActive,
                            @IsOnline,
                            @CreatedAt,
                            @UpdatedAt
                        );
                        SELECT SCOPE_IDENTITY();",
                        connection
                    );

                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@AgencyApplicationId", applicationId);
                    command.Parameters.AddWithValue(
                        "@Website",
                        (object)model.Website ?? DBNull.Value
                    );
                    command.Parameters.AddWithValue("@Email", (object)model.Email ?? DBNull.Value);
                    command.Parameters.AddWithValue(
                        "@LogoUrl",
                        (object)model.LogoUrl ?? DBNull.Value
                    );
                    command.Parameters.AddWithValue(
                        "@CoverImageUrl",
                        (object)model.CoverImageUrl ?? DBNull.Value
                    );
                    command.Parameters.AddWithValue(
                        "@OfficeHours",
                        (object)model.OfficeHours ?? DBNull.Value
                    );
                    command.Parameters.AddWithValue("@Languages", languagesJson);
                    command.Parameters.AddWithValue(
                        "@Specializations",
                        (object)model.Specializations ?? DBNull.Value
                    );
                    command.Parameters.AddWithValue("@SocialMediaLinks", socialMediaLinksJson);
                    command.Parameters.AddWithValue("@TeamMembers", teamMembersJson);
                    command.Parameters.AddWithValue("@Certifications", certificationsJson);
                    command.Parameters.AddWithValue("@Awards", awardsJson);
                    command.Parameters.AddWithValue("@Testimonials", testimonialsJson);
                    command.Parameters.AddWithValue(
                        "@TermsAndConditions",
                        (object)model.TermsAndConditions ?? DBNull.Value
                    );
                    command.Parameters.AddWithValue(
                        "@PrivacyPolicy",
                        (object)model.PrivacyPolicy ?? DBNull.Value
                    );
                    command.Parameters.AddWithValue("@Rating", 0); // Default rating
                    command.Parameters.AddWithValue("@TotalReviews", 0); // Default total reviews
                    command.Parameters.AddWithValue("@TotalBookings", 0); // Default total bookings
                    command.Parameters.AddWithValue("@TotalDeals", 0); // Default total deals
                    command.Parameters.AddWithValue("@LastActive", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@IsOnline", false); // Default offline status
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                    var profileId = Convert.ToInt32(await command.ExecuteScalarAsync());

                    return await GetAgencyProfileById(profileId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating agency profile");
                throw;
            }
        }

        private async Task<int> GetAgencyApplicationId(string userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "SELECT TOP 1 Id FROM AgencyApplications WHERE UserId = @UserId ORDER BY CreatedAt DESC",
                    connection
                );
                command.Parameters.AddWithValue("@UserId", userId);
                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        public async Task<AgencyProfileResponseDTO> GetAgencyProfileById(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT p.*, 
                           a.AgencyName,
                           a.Address,
                           a.PhoneNumber,
                           a.Description,
                           a.BusinessRegistrationNumber,
                           a.CreatedAt as ApplicationCreatedAt,
                           a.ReviewedAt,
                           a.IsApproved,
                           a.RejectionReason,
                           a.ReviewedBy
                    FROM AgencyProfiles p
                    INNER JOIN AgencyApplications a ON p.AgencyApplicationId = a.Id
                    WHERE p.Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToAgencyProfileResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agency profile");
                throw;
            }
        }

        public async Task<AgencyProfileResponseDTO> GetAgencyProfileByUserId(string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT p.*, 
                           a.AgencyName,
                           a.Address,
                           a.PhoneNumber,
                           a.Description,
                           a.BusinessRegistrationNumber,
                           a.CreatedAt as ApplicationCreatedAt,
                           a.ReviewedAt,
                           a.IsApproved,
                           a.RejectionReason,
                           a.ReviewedBy
                    FROM AgencyProfiles p
                    INNER JOIN AgencyApplications a ON p.AgencyApplicationId = a.Id
                    WHERE p.UserId = @UserId";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToAgencyProfileResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agency profile");
                throw;
            }
        }

        public async Task<bool> UpdateAgencyProfile(
            int id,
            UpdateAgencyProfileDTO model,
            string userId
        )
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Convert lists to JSON strings
                var languagesJson =
                    model.Languages != null
                        ? JsonSerializer.Serialize(model.Languages)
                        : (object)DBNull.Value;
                var socialMediaLinksJson =
                    model.SocialMediaLinks != null
                        ? JsonSerializer.Serialize(model.SocialMediaLinks)
                        : (object)DBNull.Value;
                var teamMembersJson =
                    model.TeamMembers != null
                        ? JsonSerializer.Serialize(model.TeamMembers)
                        : (object)DBNull.Value;
                var certificationsJson =
                    model.Certifications != null
                        ? JsonSerializer.Serialize(model.Certifications)
                        : (object)DBNull.Value;
                var awardsJson =
                    model.Awards != null
                        ? JsonSerializer.Serialize(model.Awards)
                        : (object)DBNull.Value;
                var testimonialsJson =
                    model.Testimonials != null
                        ? JsonSerializer.Serialize(model.Testimonials)
                        : (object)DBNull.Value;

                const string sql =
                    @"
                    UPDATE AgencyProfiles
                    SET Website = ISNULL(@Website, Website),
                        Email = ISNULL(@Email, Email),
                        LogoUrl = ISNULL(@LogoUrl, LogoUrl),
                        CoverImageUrl = ISNULL(@CoverImageUrl, CoverImageUrl),
                        OfficeHours = ISNULL(@OfficeHours, OfficeHours),
                        Languages = ISNULL(@Languages, Languages),
                        Specializations = ISNULL(@Specializations, Specializations),
                        SocialMediaLinks = ISNULL(@SocialMediaLinks, SocialMediaLinks),
                        TeamMembers = ISNULL(@TeamMembers, TeamMembers),
                        Certifications = ISNULL(@Certifications, Certifications),
                        Awards = ISNULL(@Awards, Awards),
                        Testimonials = ISNULL(@Testimonials, Testimonials),
                        TermsAndConditions = ISNULL(@TermsAndConditions, TermsAndConditions),
                        PrivacyPolicy = ISNULL(@PrivacyPolicy, PrivacyPolicy),
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id AND UserId = @UserId";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Website", (object)model.Website ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", (object)model.Email ?? DBNull.Value);
                command.Parameters.AddWithValue("@LogoUrl", (object)model.LogoUrl ?? DBNull.Value);
                command.Parameters.AddWithValue(
                    "@CoverImageUrl",
                    (object)model.CoverImageUrl ?? DBNull.Value
                );
                command.Parameters.AddWithValue(
                    "@OfficeHours",
                    (object)model.OfficeHours ?? DBNull.Value
                );
                command.Parameters.AddWithValue("@Languages", languagesJson);
                command.Parameters.AddWithValue(
                    "@Specializations",
                    (object)model.Specializations ?? DBNull.Value
                );
                command.Parameters.AddWithValue("@SocialMediaLinks", socialMediaLinksJson);
                command.Parameters.AddWithValue("@TeamMembers", teamMembersJson);
                command.Parameters.AddWithValue("@Certifications", certificationsJson);
                command.Parameters.AddWithValue("@Awards", awardsJson);
                command.Parameters.AddWithValue("@Testimonials", testimonialsJson);
                command.Parameters.AddWithValue(
                    "@TermsAndConditions",
                    (object)model.TermsAndConditions ?? DBNull.Value
                );
                command.Parameters.AddWithValue(
                    "@PrivacyPolicy",
                    (object)model.PrivacyPolicy ?? DBNull.Value
                );

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agency profile");
                throw;
            }
        }

        public async Task<bool> UpdateAgencyOnlineStatus(string userId, bool isOnline)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    UPDATE AgencyProfiles
                    SET IsOnline = @IsOnline,
                        LastActive = CASE WHEN @IsOnline = 0 THEN GETUTCDATE() ELSE LastActive END,
                        UpdatedAt = GETUTCDATE()
                    WHERE UserId = @UserId";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@IsOnline", isOnline);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agency online status");
                throw;
            }
        }

        public async Task<bool> UpdateAgencyLastActive(string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    UPDATE AgencyProfiles
                    SET LastActive = GETUTCDATE(),
                        UpdatedAt = GETUTCDATE()
                    WHERE UserId = @UserId";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agency last active");
                throw;
            }
        }

        public async Task<bool> IncrementAgencyStats(string userId, string statType)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql =
                    @"
                    UPDATE AgencyProfiles
                    SET ";

                switch (statType.ToLower())
                {
                    case "reviews":
                        sql += "TotalReviews = TotalReviews + 1";
                        break;
                    case "bookings":
                        sql += "TotalBookings = TotalBookings + 1";
                        break;
                    case "deals":
                        sql += "TotalDeals = TotalDeals + 1";
                        break;
                    default:
                        throw new ArgumentException("Invalid stat type");
                }

                sql +=
                    @",
                    UpdatedAt = GETUTCDATE()
                    WHERE UserId = @UserId";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing agency stats");
                throw;
            }
        }

        private AgencyProfileResponseDTO MapToAgencyProfileResponseDto(SqlDataReader reader)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return new AgencyProfileResponseDTO
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                AgencyApplicationId = reader.GetInt32(reader.GetOrdinal("AgencyApplicationId")),
                Website = reader.IsDBNull(reader.GetOrdinal("Website"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Website")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Email")),
                LogoUrl = reader.IsDBNull(reader.GetOrdinal("LogoUrl"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("LogoUrl")),
                CoverImageUrl = reader.IsDBNull(reader.GetOrdinal("CoverImageUrl"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("CoverImageUrl")),
                OfficeHours = reader.IsDBNull(reader.GetOrdinal("OfficeHours"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OfficeHours")),
                Languages = reader.IsDBNull(reader.GetOrdinal("Languages"))
                    ? null
                    : SafeDeserializeJson<List<string>>(
                        reader.GetString(reader.GetOrdinal("Languages"))
                    ),
                Specializations = reader.IsDBNull(reader.GetOrdinal("Specializations"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Specializations")),
                SocialMediaLinks = reader.IsDBNull(reader.GetOrdinal("SocialMediaLinks"))
                    ? null
                    : JsonSerializer.Deserialize<List<SocialMediaLinkDTO>>(
                        reader.GetString(reader.GetOrdinal("SocialMediaLinks")),
                        options
                    ),
                TeamMembers = reader.IsDBNull(reader.GetOrdinal("TeamMembers"))
                    ? null
                    : JsonSerializer.Deserialize<List<TeamMemberDTO>>(
                        reader.GetString(reader.GetOrdinal("TeamMembers")),
                        options
                    ),
                Certifications = reader.IsDBNull(reader.GetOrdinal("Certifications"))
                    ? null
                    : JsonSerializer.Deserialize<List<CertificationDTO>>(
                        reader.GetString(reader.GetOrdinal("Certifications")),
                        options
                    ),
                Awards = reader.IsDBNull(reader.GetOrdinal("Awards"))
                    ? null
                    : JsonSerializer.Deserialize<List<AwardDTO>>(
                        reader.GetString(reader.GetOrdinal("Awards")),
                        options
                    ),
                Testimonials = reader.IsDBNull(reader.GetOrdinal("Testimonials"))
                    ? null
                    : JsonSerializer.Deserialize<List<TestimonialDTO>>(
                        reader.GetString(reader.GetOrdinal("Testimonials")),
                        options
                    ),
                TermsAndConditions = reader.IsDBNull(reader.GetOrdinal("TermsAndConditions"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("TermsAndConditions")),
                PrivacyPolicy = reader.IsDBNull(reader.GetOrdinal("PrivacyPolicy"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("PrivacyPolicy")),
                Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                TotalReviews = reader.GetInt32(reader.GetOrdinal("TotalReviews")),
                TotalBookings = reader.GetInt32(reader.GetOrdinal("TotalBookings")),
                TotalDeals = reader.GetInt32(reader.GetOrdinal("TotalDeals")),
                LastActive = reader.IsDBNull(reader.GetOrdinal("LastActive"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("LastActive")),
                IsOnline = reader.GetBoolean(reader.GetOrdinal("IsOnline")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                // Agency Application Fields
                AgencyName = reader.IsDBNull(reader.GetOrdinal("AgencyName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("AgencyName")),
                Address = reader.IsDBNull(reader.GetOrdinal("Address"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Address")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description")),
                BusinessRegistrationNumber = reader.IsDBNull(
                    reader.GetOrdinal("BusinessRegistrationNumber")
                )
                    ? null
                    : reader.GetString(reader.GetOrdinal("BusinessRegistrationNumber")),
                ApplicationCreatedAt = reader.GetDateTime(
                    reader.GetOrdinal("ApplicationCreatedAt")
                ),
                ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
                IsApproved = reader.GetBoolean(reader.GetOrdinal("IsApproved")),
                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("RejectionReason")),
                ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ReviewedBy")),
            };
        }

        public async Task<bool> UpdateAgencyTotalDeals(string userId, int change)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (
                        var command = new SqlCommand(
                            @"UPDATE AgencyProfiles 
                          SET TotalDeals = TotalDeals + @change
                          WHERE UserId = @userId",
                            connection
                        )
                    )
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@change", change);
                        return await command.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agency total deals");
                return false;
            }
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
                Currency = reader.IsDBNull(reader.GetOrdinal("Currency"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Currency")),
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
            try
            {
                // Helper method to get string from reader that handles DBNull
                string GetStringOrEmpty(string columnName)
                {
                    return reader.IsDBNull(reader.GetOrdinal(columnName))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal(columnName));
                }

                // Helper method to safely deserialize JSON
                T SafeJsonDeserialize<T>(string columnName)
                    where T : class, new()
                {
                    try
                    {
                        string json = GetStringOrEmpty(columnName);

                        if (string.IsNullOrWhiteSpace(json))
                            return new T();

                        // Handle potential bad JSON data
                        if (!json.TrimStart().StartsWith("[") && !json.TrimStart().StartsWith("{"))
                            return new T();

                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                        };

                        return JsonSerializer.Deserialize<T>(json, options) ?? new T();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error deserializing JSON for column {Column}. Value: {Value}",
                            columnName,
                            reader.IsDBNull(reader.GetOrdinal(columnName))
                                ? "NULL"
                                : GetStringOrEmpty(columnName)
                        );
                        return new T();
                    }
                }

                // Helper method to get decimal with default value
                decimal GetDecimalOrDefault(string columnName, decimal defaultValue = 0)
                {
                    return reader.IsDBNull(reader.GetOrdinal(columnName))
                        ? defaultValue
                        : reader.GetDecimal(reader.GetOrdinal(columnName));
                }

                // Helper method to get int with default value
                int GetIntOrDefault(string columnName, int defaultValue = 0)
                {
                    return reader.IsDBNull(reader.GetOrdinal(columnName))
                        ? defaultValue
                        : reader.GetInt32(reader.GetOrdinal(columnName));
                }

                // Helper method to get bool with default value
                bool GetBoolOrDefault(string columnName, bool defaultValue = false)
                {
                    return reader.IsDBNull(reader.GetOrdinal(columnName))
                        ? defaultValue
                        : reader.GetBoolean(reader.GetOrdinal(columnName));
                }

                return new DealResponseDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Title = GetStringOrEmpty("Title"),
                    LocationId = reader.GetInt32(reader.GetOrdinal("LocationId")),
                    UserId = GetStringOrEmpty("UserId"),
                    Price = GetDecimalOrDefault("Price"),
                    DiscountedPrice = GetDecimalOrDefault("DiscountedPrice"),
                    DiscountPercentage = GetIntOrDefault("DiscountPercentage"),
                    Rating = GetDecimalOrDefault("Rating"),
                    DaysCount = GetIntOrDefault("DaysCount"),
                    NightsCount = GetIntOrDefault("NightsCount"),
                    Description = GetStringOrEmpty("Description"),
                    Photos = SafeJsonDeserialize<List<string>>("Photos"),
                    ElderlyFriendly = GetBoolOrDefault("ElderlyFriendly"),
                    InternetIncluded = GetBoolOrDefault("InternetIncluded"),
                    TravelIncluded = GetBoolOrDefault("TravelIncluded"),
                    MealsIncluded = GetBoolOrDefault("MealsIncluded"),
                    SightseeingIncluded = GetBoolOrDefault("SightseeingIncluded"),
                    StayIncluded = GetBoolOrDefault("StayIncluded"),
                    AirTransfer = GetBoolOrDefault("AirTransfer"),
                    RoadTransfer = GetBoolOrDefault("RoadTransfer"),
                    TrainTransfer = GetBoolOrDefault("TrainTransfer"),
                    TravelCostIncluded = GetBoolOrDefault("TravelCostIncluded"),
                    GuideIncluded = GetBoolOrDefault("GuideIncluded"),
                    PhotographyIncluded = GetBoolOrDefault("PhotographyIncluded"),
                    InsuranceIncluded = GetBoolOrDefault("InsuranceIncluded"),
                    VisaIncluded = GetBoolOrDefault("VisaIncluded"),
                    Itinerary = SafeJsonDeserialize<List<ItineraryDay>>("Itinerary"),
                    PackageOptions = SafeJsonDeserialize<List<PackageOption>>("PackageOptions"),
                    MapUrl = GetStringOrEmpty("MapUrl"),
                    Policies = SafeJsonDeserialize<List<Policy>>("Policies"),
                    PackageType = GetStringOrEmpty("PackageType"),
                    IsActive = GetBoolOrDefault("IsActive"),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    ClickCount = GetIntOrDefault("ClickCount"),
                    ViewCount = GetIntOrDefault("ViewCount"),
                    BookingCount = GetIntOrDefault("BookingCount"),
                    LastClicked = reader.IsDBNull(reader.GetOrdinal("LastClicked"))
                        ? DateTime.MinValue
                        : reader.GetDateTime(reader.GetOrdinal("LastClicked")),
                    LastViewed = reader.IsDBNull(reader.GetOrdinal("LastViewed"))
                        ? DateTime.MinValue
                        : reader.GetDateTime(reader.GetOrdinal("LastViewed")),
                    LastBooked = reader.IsDBNull(reader.GetOrdinal("LastBooked"))
                        ? DateTime.MinValue
                        : reader.GetDateTime(reader.GetOrdinal("LastBooked")),
                    RelevanceScore = GetDecimalOrDefault("RelevanceScore"),
                    SearchKeywords = reader.IsDBNull(reader.GetOrdinal("SearchKeywords"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("SearchKeywords")),
                    Tags = SafeJsonDeserialize<List<string>>("Tags"),
                    Headlines = reader.IsDBNull(reader.GetOrdinal("Headlines"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Headlines")),
                    IsFeatured = GetBoolOrDefault("IsFeatured"),
                    FeaturedUntil = reader.IsDBNull(reader.GetOrdinal("FeaturedUntil"))
                        ? DateTime.MinValue
                        : reader.GetDateTime(reader.GetOrdinal("FeaturedUntil")),
                    Priority = GetIntOrDefault("Priority"),
                    DifficultyLevel = GetStringOrEmpty("DifficultyLevel"),
                    MaxGroupSize = GetIntOrDefault("MaxGroupSize"),
                    MinGroupSize = GetIntOrDefault("MinGroupSize"),
                    IsInstantBooking = GetBoolOrDefault("IsInstantBooking"),
                    IsLastMinuteDeal = GetBoolOrDefault("IsLastMinuteDeal"),
                    ValidFrom = reader.IsDBNull(reader.GetOrdinal("ValidFrom"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ValidFrom")),
                    ValidUntil = reader.IsDBNull(reader.GetOrdinal("ValidUntil"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ValidUntil")),
                    CancellationPolicy = GetStringOrEmpty("CancellationPolicy"),
                    RefundPolicy = GetStringOrEmpty("RefundPolicy"),
                    Languages = SafeJsonDeserialize<List<string>>("Languages"),
                    Requirements = SafeJsonDeserialize<List<string>>("Requirements"),
                    Restrictions = SafeJsonDeserialize<List<string>>("Restrictions"),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error mapping deal from database. Exception: {Exception}",
                    ex.Message
                );
                throw;
            }
        }

        private void AddDealParameters(SqlCommand command, DealCreateDto deal)
        {
            command.Parameters.AddWithValue("@Title", deal.Title);
            command.Parameters.AddWithValue("@LocationId", deal.LocationId);
            command.Parameters.AddWithValue("@Price", deal.Price);
            command.Parameters.AddWithValue("@DiscountedPrice", deal.DiscountedPrice ?? 0);
            command.Parameters.AddWithValue("@DiscountPercentage", deal.DiscountPercentage ?? 0);
            command.Parameters.AddWithValue("@Rating", deal.Rating ?? 0);
            command.Parameters.AddWithValue("@DaysCount", deal.DaysCount);
            command.Parameters.AddWithValue("@NightsCount", deal.NightsCount);
            command.Parameters.AddWithValue("@Description", deal.Description);
            command.Parameters.AddWithValue(
                "@Photos",
                JsonSerializer.Serialize(deal.Photos ?? new List<string>())
            );
            command.Parameters.AddWithValue("@ElderlyFriendly", deal.ElderlyFriendly ?? false);
            command.Parameters.AddWithValue("@InternetIncluded", deal.InternetIncluded ?? false);
            command.Parameters.AddWithValue("@TravelIncluded", deal.TravelIncluded ?? false);
            command.Parameters.AddWithValue("@MealsIncluded", deal.MealsIncluded ?? false);
            command.Parameters.AddWithValue(
                "@SightseeingIncluded",
                deal.SightseeingIncluded ?? false
            );
            command.Parameters.AddWithValue("@StayIncluded", deal.StayIncluded ?? false);
            command.Parameters.AddWithValue("@AirTransfer", deal.AirTransfer ?? false);
            command.Parameters.AddWithValue("@RoadTransfer", deal.RoadTransfer ?? false);
            command.Parameters.AddWithValue("@TrainTransfer", deal.TrainTransfer ?? false);
            command.Parameters.AddWithValue(
                "@TravelCostIncluded",
                deal.TravelCostIncluded ?? false
            );
            command.Parameters.AddWithValue("@GuideIncluded", deal.GuideIncluded ?? false);
            command.Parameters.AddWithValue(
                "@PhotographyIncluded",
                deal.PhotographyIncluded ?? false
            );
            command.Parameters.AddWithValue("@InsuranceIncluded", deal.InsuranceIncluded ?? false);
            command.Parameters.AddWithValue("@VisaIncluded", deal.VisaIncluded ?? false);
            command.Parameters.AddWithValue(
                "@Itinerary",
                JsonSerializer.Serialize(deal.Itinerary ?? new List<ItineraryDay>())
            );
            command.Parameters.AddWithValue(
                "@PackageOptions",
                JsonSerializer.Serialize(deal.PackageOptions ?? new List<PackageOption>())
            );
            command.Parameters.AddWithValue("@MapUrl", deal.MapUrl ?? string.Empty);
            command.Parameters.AddWithValue(
                "@Policies",
                JsonSerializer.Serialize(deal.Policies ?? new List<Policy>())
            );
            command.Parameters.AddWithValue("@PackageType", deal.PackageType);
            command.Parameters.AddWithValue("@IsActive", deal.IsActive);
            command.Parameters.AddWithValue("@Headlines", (object)deal.Headlines ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "@Tags",
                JsonSerializer.Serialize(deal.Tags ?? new List<string>())
            );
            command.Parameters.AddWithValue(
                "@Seasons",
                JsonSerializer.Serialize(deal.Seasons ?? new List<string>())
            );
            command.Parameters.AddWithValue("@DifficultyLevel", deal.DifficultyLevel ?? "Easy");
            command.Parameters.AddWithValue("@MaxGroupSize", deal.MaxGroupSize ?? 0);
            command.Parameters.AddWithValue("@MinGroupSize", deal.MinGroupSize ?? 0);
            command.Parameters.AddWithValue("@IsInstantBooking", deal.IsInstantBooking ?? false);
            command.Parameters.AddWithValue("@IsLastMinuteDeal", deal.IsLastMinuteDeal ?? false);
            command.Parameters.AddWithValue("@ValidFrom", deal.ValidFrom);
            command.Parameters.AddWithValue("@ValidUntil", deal.ValidUntil);
            command.Parameters.AddWithValue(
                "@CancellationPolicy",
                deal.CancellationPolicy ?? string.Empty
            );
            command.Parameters.AddWithValue("@RefundPolicy", deal.RefundPolicy ?? string.Empty);
            command.Parameters.AddWithValue(
                "@Languages",
                JsonSerializer.Serialize(deal.Languages ?? new List<string>())
            );
            command.Parameters.AddWithValue(
                "@Requirements",
                JsonSerializer.Serialize(deal.Requirements ?? new List<string>())
            );
            command.Parameters.AddWithValue(
                "@Restrictions",
                JsonSerializer.Serialize(deal.Restrictions ?? new List<string>())
            );
            command.Parameters.AddWithValue("@UserId", deal.UserId);
            command.Parameters.AddWithValue("@CreatedAt", deal.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", deal.UpdatedAt);
        }

        #endregion

        #region FAQ Operations

        public async Task<IEnumerable<FAQResponseDTO>> GetFAQs()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT Id, Question, Answer, OrderIndex, Category, IsActive, CreatedAt, UpdatedAt
                    FROM FAQs
                    WHERE IsActive = 1
                    ORDER BY OrderIndex ASC, CreatedAt DESC";

                using var command = new SqlCommand(sql, connection);
                var faqs = new List<FAQResponseDTO>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    faqs.Add(MapToFAQResponseDto(reader));
                }

                return faqs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FAQs");
                throw;
            }
        }

        public async Task<IEnumerable<FAQResponseDTO>> SearchFAQs(string query)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT Id, Question, Answer, OrderIndex, Category, IsActive, CreatedAt, UpdatedAt
                    FROM FAQs
                    WHERE IsActive = 1 
                    AND (Question LIKE @Query OR Answer LIKE @Query OR Category LIKE @Query)
                    ORDER BY OrderIndex ASC, CreatedAt DESC";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Query", $"%{query}%");
                var faqs = new List<FAQResponseDTO>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    faqs.Add(MapToFAQResponseDto(reader));
                }

                return faqs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching FAQs");
                throw;
            }
        }

        public async Task<FAQResponseDTO> CreateFAQ(FAQCreateDTO faq)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    INSERT INTO FAQs (Question, Category, OrderIndex, IsActive, CreatedAt)
                    OUTPUT INSERTED.*
                    VALUES (@Question, @Category, 
                           (SELECT ISNULL(MAX(OrderIndex), 0) + 1 FROM FAQs), 
                           1, GETUTCDATE())";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Question", faq.Question);
                command.Parameters.AddWithValue("@Category", (object)faq.Category ?? DBNull.Value);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToFAQResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FAQ");
                throw;
            }
        }

        public async Task<FAQResponseDTO> UpdateFAQ(int id, FAQUpdateDTO faq)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    UPDATE FAQs
                    SET Answer = @Answer,
                        OrderIndex = @OrderIndex,
                        IsActive = @IsActive,
                        UpdatedAt = GETUTCDATE()
                    OUTPUT INSERTED.*
                    WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Answer", faq.Answer);
                command.Parameters.AddWithValue("@OrderIndex", faq.OrderIndex);
                command.Parameters.AddWithValue("@IsActive", faq.IsActive);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToFAQResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FAQ");
                throw;
            }
        }

        private FAQResponseDTO MapToFAQResponseDto(SqlDataReader reader)
        {
            return new FAQResponseDTO
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Question = reader.GetString(reader.GetOrdinal("Question")),
                Answer = reader.IsDBNull(reader.GetOrdinal("Answer"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Answer")),
                OrderIndex = reader.GetInt32(reader.GetOrdinal("OrderIndex")),
                Category = reader.IsDBNull(reader.GetOrdinal("Category"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Category")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            };
        }

        #endregion

        #region Support Ticket Operations

        public async Task<IEnumerable<SupportTicketResponseDTO>> GetSupportTickets(
            string? status = null
        )
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql =
                    @"
                    SELECT Id, Name, Email, ProblemTitle, ProblemDescription, 
                           Status, AdminResponse, CreatedAt, UpdatedAt, ResolvedAt
                    FROM SupportTickets
                    WHERE (@Status IS NULL OR Status = @Status)
                    ORDER BY CreatedAt DESC";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);
                var tickets = new List<SupportTicketResponseDTO>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tickets.Add(MapToSupportTicketResponseDto(reader));
                }

                return tickets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting support tickets");
                throw;
            }
        }

        public async Task<SupportTicketResponseDTO> GetSupportTicketById(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    SELECT Id, Name, Email, ProblemTitle, ProblemDescription, 
                           Status, AdminResponse, CreatedAt, UpdatedAt, ResolvedAt
                    FROM SupportTickets
                    WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToSupportTicketResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting support ticket");
                throw;
            }
        }

        public async Task<SupportTicketResponseDTO> CreateSupportTicket(
            SupportTicketCreateDTO ticket
        )
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    INSERT INTO SupportTickets (Name, Email, ProblemTitle, ProblemDescription, 
                                              Status, CreatedAt)
                    OUTPUT INSERTED.*
                    VALUES (@Name, @Email, @ProblemTitle, @ProblemDescription, 
                           'Open', GETUTCDATE())";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Name", ticket.Name);
                command.Parameters.AddWithValue("@Email", ticket.Email);
                command.Parameters.AddWithValue("@ProblemTitle", ticket.ProblemTitle);
                command.Parameters.AddWithValue("@ProblemDescription", ticket.ProblemDescription);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToSupportTicketResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating support ticket");
                throw;
            }
        }

        public async Task<SupportTicketResponseDTO> UpdateSupportTicket(
            int id,
            SupportTicketUpdateDTO ticket
        )
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql =
                    @"
                    UPDATE SupportTickets
                    SET AdminResponse = @AdminResponse,
                        Status = 'Resolved',
                        UpdatedAt = GETUTCDATE(),
                        ResolvedAt = GETUTCDATE()
                    OUTPUT INSERTED.*
                    WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@AdminResponse", ticket.AdminResponse);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToSupportTicketResponseDto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating support ticket");
                throw;
            }
        }

        public async Task<bool> UpdateSupportTicketStatus(int id, string status)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql =
                    @"
                    UPDATE SupportTickets
                    SET Status = @Status,
                        UpdatedAt = GETUTCDATE(),
                        ResolvedAt = CASE WHEN @Status = 'Resolved' THEN GETUTCDATE() ELSE ResolvedAt END
                    WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Status", status);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating support ticket status");
                throw;
            }
        }

        private SupportTicketResponseDTO MapToSupportTicketResponseDto(SqlDataReader reader)
        {
            return new SupportTicketResponseDTO
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                ProblemTitle = reader.GetString(reader.GetOrdinal("ProblemTitle")),
                ProblemDescription = reader.GetString(reader.GetOrdinal("ProblemDescription")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                AdminResponse = reader.IsDBNull(reader.GetOrdinal("AdminResponse"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("AdminResponse")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                ResolvedAt = reader.IsDBNull(reader.GetOrdinal("ResolvedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("ResolvedAt")),
            };
        }

        #endregion
    }
}
