using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        [Required]
        public string StripePaymentId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "USD";

        [Required]
        public string Status { get; set; } // pending, succeeded, failed, refunded

        public string? PaymentMethod { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }

        public string? ErrorMessage { get; set; }
        public string? RefundReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? RefundedAt { get; set; }
    }
}
