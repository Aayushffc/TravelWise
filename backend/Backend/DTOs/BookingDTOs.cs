using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class CreateBookingDTO
    {
        [Required]
        public string AgencyId { get; set; }

        [Required]
        public int DealId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int NumberOfPeople { get; set; }

        public DateTime? TravelDate { get; set; }
        public string? SpecialRequirements { get; set; }
        public string? Notes { get; set; }
    }

    public class RejectBookingDTO
    {
        public string? Reason { get; set; }
    }

    public class CancelBookingDTO
    {
        public string? Reason { get; set; }
    }

    public class SendMessageDTO
    {
        [Required]
        public string ReceiverId { get; set; }

        [Required]
        public string Message { get; set; }

        public string? MessageType { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
    }

    public class BookingResponseDTO
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? AgencyId { get; set; }
        public string? AgencyName { get; set; }
        public int DealId { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int NumberOfPeople { get; set; }
        public DateTime? TravelDate { get; set; }
        public string? SpecialRequirements { get; set; }
        public string? Notes { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool HasUnreadMessages { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? PaymentStatus { get; set; }
    }

    public class ChatMessageResponseDTO
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string? SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? ReceiverId { get; set; }
        public string? Message { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsRead { get; set; }
        public string? MessageType { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
    }
}
