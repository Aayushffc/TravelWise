using System.ComponentModel.DataAnnotations;
using Backend.Models.Auth;

namespace Backend.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string AgencyId { get; set; }
        public ApplicationUser Agency { get; set; }

        [Required]
        public int DealId { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Completed, Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public string? RejectionReason { get; set; }
        public string? CancellationReason { get; set; }

        // Booking details
        public int NumberOfPeople { get; set; }
        public DateTime? TravelDate { get; set; }
        public string? SpecialRequirements { get; set; }
        public string? Notes { get; set; }

        // Contact information
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string BookingMessage { get; set; }

        // Chat related
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool HasUnreadMessages { get; set; }
        public string? LastMessageBy { get; set; }

        // Payment related
        public decimal? TotalAmount { get; set; }
        public string? PaymentStatus { get; set; } // Pending, Paid, Refunded
        public string? PaymentMethod { get; set; }
        public string? PaymentId { get; set; }
        public DateTime? PaymentDate { get; set; }

        // Rating and review
        public int? Rating { get; set; }
        public string? Review { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
