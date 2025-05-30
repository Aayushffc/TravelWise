using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Models.Auth;

namespace Backend.Models
{
    public class AgencyStripeConnect
    {
        public int Id { get; set; }

        [Required]
        public string AgencyId { get; set; }

        [ForeignKey("AgencyId")]
        public ApplicationUser Agency { get; set; }

        [Required]
        public string StripeAccountId { get; set; }

        public string? StripeAccountStatus { get; set; }
        public bool IsEnabled { get; set; }
        public string? PayoutsEnabled { get; set; }
        public string? ChargesEnabled { get; set; }
        public string? DetailsSubmitted { get; set; }
        public string? Requirements { get; set; } // JSON string of requirements
        public string? Capabilities { get; set; } // JSON string of capabilities
        public string? BusinessType { get; set; }
        public string? BusinessProfile { get; set; } // JSON string of business profile
        public string? ExternalAccounts { get; set; } // JSON string of external accounts
        public string? VerificationStatus { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
    }
}