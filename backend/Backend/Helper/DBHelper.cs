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

                _logger.LogInformation("Executing SQL query: {Sql}", sql);
                _logger.LogInformation(
                    "Parameters: {Parameters}",
                    string.Join(", ", parameters.Select(p => $"{p.ParameterName}={p.Value}"))
                );

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

                        _logger.LogInformation(
                            "Raw JSON data - Photos: {Photos}, Headlines: {Headlines}, Tags: {Tags}, Seasons: {Seasons}",
                            photosJson,
                            headlinesJson,
                            tagsJson,
                            seasonsJson
                        );

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
                            Headlines = SafeDeserializeJson<List<string>>(headlinesJson),
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
                    parameters.Add(
                        new SqlParameter("@Headlines", JsonSerializer.Serialize(deal.Headlines))
                    );
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
                    Headlines = SafeJsonDeserialize<List<string>>("Headlines"),
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
            command.Parameters.AddWithValue(
                "@Headlines",
                JsonSerializer.Serialize(deal.Headlines ?? new List<string>())
            );
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
    }
}
