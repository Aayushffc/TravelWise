using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class CreatePaymentIntentDTO
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "USD";

        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
    }

    public class PaymentIntentResponseDTO
    {
        public string? ClientSecret { get; set; }
        public string? PaymentIntentId { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
    }

    public class PaymentResponseDTO
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string? StripePaymentId { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? RefundedAt { get; set; }
    }

    public class RefundPaymentDTO
    {
        [Required]
        public int PaymentId { get; set; }
        public string? Reason { get; set; }
    }
}
