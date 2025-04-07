using System.ComponentModel.DataAnnotations;
using Backend.Models.Auth;

namespace Backend.Models.Auth
{
    public class AgencyProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public int AgencyApplicationId { get; set; }
        public AgencyApplication AgencyApplication { get; set; }

        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? OfficeHours { get; set; }
        public string? Languages { get; set; }
        public string? Specializations { get; set; }
        public string? SocialMediaLinks { get; set; } // JSON string of social media links
        public string? TeamMembers { get; set; } // JSON string of team members
        public string? Certifications { get; set; } // JSON string of certifications
        public string? Awards { get; set; } // JSON string of awards
        public string? Testimonials { get; set; } // JSON string of testimonials
        public string? TermsAndConditions { get; set; }
        public string? PrivacyPolicy { get; set; }
        public int Rating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalBookings { get; set; }
        public int TotalDeals { get; set; }
        public DateTime? LastActive { get; set; }
        public bool IsOnline { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
