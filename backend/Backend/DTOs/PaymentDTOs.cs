using System;
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
        public string? AgencyId { get; set; }
        public string? AgencyStripeAccountId { get; set; }
        public decimal? CommissionPercentage { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Description { get; set; }
        public DateTime? PaymentDeadline { get; set; }
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
        public string? AgencyId { get; set; }
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
        public string? Reason { get; set; }
    }

    public class AcceptPaymentDTO
    {
        [Required]
        public string? BankAccountNumber { get; set; }

        [Required]
        public string? BankRoutingNumber { get; set; }

        [Required]
        public string? BankName { get; set; }

        [Required]
        public string? AccountHolderName { get; set; }

        [Required]
        public string? AccountType { get; set; } // "checking" or "savings"
    }

    public class RejectPaymentDTO
    {
        [Required]
        public string? Reason { get; set; }
    }

    public class AgencyPaymentSetupDTO
    {
        [Required]
        public string? BankAccountNumber { get; set; }

        [Required]
        public string? BankRoutingNumber { get; set; }

        [Required]
        public string? BankName { get; set; }

        [Required]
        public string? AccountHolderName { get; set; }

        [Required]
        public string? AccountType { get; set; } // "checking" or "savings"

        [Required]
        public string? TaxId { get; set; }

        [Required]
        public string? BusinessName { get; set; }

        [Required]
        public string? BusinessAddress { get; set; }
    }

    public class PaymentRequestDTO
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string? AgencyId { get; set; }
        public string? UserId { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? AgencyStripeAccountId { get; set; }
        public decimal? CommissionPercentage { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? RefundedAt { get; set; }
    }

    public class CreatePaymentRequestDTO
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "USD";

        public string? Description { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public decimal? CommissionPercentage { get; set; }
    }

    public class StripeStatusResponse
    {
        public bool IsConnected { get; set; }
        public string? AccountId { get; set; }
        public string? AccountStatus { get; set; }
        public DateTime? ConnectedAt { get; set; }
    }

    public class ConfirmPaymentDTO
    {
        [Required]
        public string PaymentMethodId { get; set; }
    }
}
