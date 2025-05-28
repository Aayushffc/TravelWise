using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Models.Auth;

namespace Backend.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required]
        public int AgencyId { get; set; }

        [ForeignKey("AgencyId")]
        public AgencyProfile? Agency { get; set; }

        [Required]
        public string? StripePaymentId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "USD";

        [Required]
        public string? Status { get; set; } // requires_payment_method, succeeded, failed, refunded

        public string? PaymentMethod { get; set; }

        // Customer information (the user making the payment)
        public string? CustomerId { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }

        // Booking customer information (the person the booking is for)
        public string? BookingCustomerEmail { get; set; }
        public string? BookingCustomerName { get; set; }

        public string? ErrorMessage { get; set; }
        public string? RefundReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? RefundedAt { get; set; }
    }
}
