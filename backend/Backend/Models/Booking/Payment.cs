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
        public string? StripePaymentId { get; set; }

        [Required]
        public string? AgencyId { get; set; }

        [ForeignKey("AgencyId")]
        public ApplicationUser? Agency { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string? Currency { get; set; }

        [Required]
        public string? Status { get; set; }

        public string? PaymentMethod { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
        public string? AgencyStripeAccountId { get; set; }
        public decimal? CommissionPercentage { get; set; }
        public decimal? CommissionAmount { get; set; }
        public string? TransferId { get; set; }
        public string? BookingCustomerEmail { get; set; }
        public string? BookingCustomerName { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RefundReason { get; set; }
        public string? Description { get; set; }
        public DateTime? PaymentDeadline { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public DateTime? TransferredAt { get; set; }
    }
}
