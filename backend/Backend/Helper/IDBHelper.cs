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
        Task<IEnumerable<DealResponseDto>> GetDeals(
            int? locationId = null,
            string? status = null,
            bool? isActive = null,
            bool? isFeatured = null,
            string? packageType = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minDays = null,
            int? maxDays = null
        );

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
            string? packageType = null,
            string? difficultyLevel = null,
            bool? isInstantBooking = null,
            bool? isLastMinuteDeal = null,
            string? status = null
        );

        Task<IEnumerable<DealResponseDto>> GetDealsByUserId(
            string userId,
            string? status = null,
            bool? isActive = null
        );

        Task<bool> IncrementDealClickCount(int id);
        Task<bool> ToggleDealStatus(int id, bool isActive);

        // Booking Operations
        Task<BookingResponseDTO> CreateBooking(CreateBookingDTO model, string userId);
        Task<IEnumerable<BookingResponseDTO>> GetBookings(string userId);
        Task<BookingResponseDTO> GetBookingById(int id, string userId);
        Task<bool> UpdateBookingStatus(int id, string status, string reason, string userId);
        Task<bool> IncrementBookingCount(int dealId);
        Task<bool> UpdateBookingLastMessage(int bookingId, string message, string userId);

        // Chat Message Operations
        Task<ChatMessageResponseDTO> SendMessage(
            int bookingId,
            SendMessageDTO model,
            string senderId
        );
        Task<IEnumerable<ChatMessageResponseDTO>> GetChatMessages(int bookingId, string userId);
        Task<bool> MarkMessageAsRead(int messageId, string userId);

        // Agency Profile Operations
        Task<AgencyProfileResponseDTO> CreateAgencyProfile(
            CreateAgencyProfileDTO model,
            string userId
        );
        Task<bool> UpdateAgencyTotalDeals(string userId, int change);
        Task<AgencyProfileResponseDTO> GetAgencyProfileById(int id);
        Task<AgencyProfileResponseDTO> GetAgencyProfileByUserId(string userId);
        Task<bool> UpdateAgencyProfile(int id, UpdateAgencyProfileDTO model, string userId);
        Task<bool> UpdateAgencyOnlineStatus(string userId, bool isOnline);
        Task<bool> UpdateAgencyLastActive(string userId);
        Task<bool> IncrementAgencyStats(string userId, string statType);
    }
}
