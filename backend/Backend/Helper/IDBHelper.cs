using Backend.DTOs;
using Backend.Models;
using Backend.Models.Auth;

namespace Backend.Helper
{
    public interface IDBHelper
    {
        Task<string> GenerateJwtToken(ApplicationUser user);
        Task<(string token, string refreshToken)> GenerateTokens(ApplicationUser user);
        Task<bool> ValidateRefreshToken(string refreshToken);
        Task<ApplicationUser?> GetUserByRefreshToken(string refreshToken);
        Task<bool> AddUserRole(ApplicationUser user);
        Task<bool> AddAgencyRole(ApplicationUser user);
        Task<bool> AddAdminRole(ApplicationUser user);
        Task<bool> IsUserAdmin(ApplicationUser user);

        // Location operations
        Task<IEnumerable<LocationResponseDto>> GetLocations();
        Task<IEnumerable<LocationResponseDto>> GetPopularLocations(int limit = 10);
        Task<LocationResponseDto> GetLocationById(int id);
        Task<LocationResponseDto> CreateLocation(LocationCreateDto location);
        Task<bool> UpdateLocation(int id, LocationUpdateDto location);
        Task<bool> DeleteLocation(int id);
        Task<bool> IncrementLocationClickCount(int id);
        Task<bool> IncrementLocationRequestCallCount(int id);

        // Deal operations
        Task<IEnumerable<DealResponseDto>> GetDeals(int? locationId = null);
        Task<DealResponseDto> GetDealById(int id);
        Task<DealResponseDto> CreateDeal(DealCreateDto deal);
        Task<bool> UpdateDeal(int id, DealUpdateDto deal);
        Task<bool> DeleteDeal(int id);
        Task<IEnumerable<DealResponseDto>> SearchDeals(
            string? searchTerm = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minDays = null,
            int? maxDays = null,
            string? packageType = null
        );
    }
}
