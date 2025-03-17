using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Backend.DTOs;
using Backend.Models;
using Backend.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", user.Id),
                new Claim("FullName", user.FullName),
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return await Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
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

        #region Location Operations

        public async Task<IEnumerable<LocationResponseDto>> GetLocations()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                SELECT Id, Name, Description, ImageUrl, IsPopular, IsActive, 
                       ClickCount, RequestCallCount, CreatedAt, UpdatedAt
                FROM Locations
                WHERE IsActive = 1
                ORDER BY Name";

            using var command = new SqlCommand(sql, connection);
            var locations = new List<LocationResponseDto>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                locations.Add(MapToLocationResponseDto(reader));
            }

            return locations;
        }

        public async Task<IEnumerable<LocationResponseDto>> GetPopularLocations(int limit = 10)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                SELECT TOP (@Limit) Id, Name, Description, ImageUrl, IsPopular, IsActive, 
                       ClickCount, RequestCallCount, CreatedAt, UpdatedAt
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
                       ClickCount, RequestCallCount, CreatedAt, UpdatedAt
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
                deals.Add(MapToDealResponseDto(reader));
            }

            return deals;
        }

        public async Task<DealResponseDto> GetDealById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                @"
                SELECT d.*, l.Name as LocationName, l.Description as LocationDescription,
                       l.ImageUrl as LocationImageUrl, l.IsPopular as LocationIsPopular,
                       l.IsActive as LocationIsActive, l.ClickCount as LocationClickCount,
                       l.RequestCallCount as LocationRequestCallCount,
                       l.CreatedAt as LocationCreatedAt, l.UpdatedAt as LocationUpdatedAt
                FROM Deals d
                INNER JOIN Locations l ON d.LocationId = l.Id
                WHERE d.Id = @Id AND d.IsActive = 1";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDealResponseDto(reader);
            }

            return null;
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
                    PackageType, IsActive, CreatedAt, UpdatedAt
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
                    @PackageType, @IsActive, GETUTCDATE(), GETUTCDATE()
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

            const string sql =
                @"
                UPDATE Deals
                SET Title = @Title,
                    LocationId = @LocationId,
                    Price = @Price,
                    DiscountedPrice = @DiscountedPrice,
                    DiscountPercentage = @DiscountPercentage,
                    Rating = @Rating,
                    DaysCount = @DaysCount,
                    NightsCount = @NightsCount,
                    StartPoint = @StartPoint,
                    EndPoint = @EndPoint,
                    Duration = @Duration,
                    Description = @Description,
                    Photos = @Photos,
                    ElderlyFriendly = @ElderlyFriendly,
                    InternetIncluded = @InternetIncluded,
                    TravelIncluded = @TravelIncluded,
                    MealsIncluded = @MealsIncluded,
                    SightseeingIncluded = @SightseeingIncluded,
                    StayIncluded = @StayIncluded,
                    AirTransfer = @AirTransfer,
                    RoadTransfer = @RoadTransfer,
                    TrainTransfer = @TrainTransfer,
                    TravelCostIncluded = @TravelCostIncluded,
                    GuideIncluded = @GuideIncluded,
                    PhotographyIncluded = @PhotographyIncluded,
                    InsuranceIncluded = @InsuranceIncluded,
                    VisaIncluded = @VisaIncluded,
                    Itinerary = @Itinerary,
                    PackageOptions = @PackageOptions,
                    MapUrl = @MapUrl,
                    Policies = @Policies,
                    PackageType = @PackageType,
                    IsActive = @IsActive,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            AddDealParameters(command, deal);

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
        }

        #endregion
    }
}
