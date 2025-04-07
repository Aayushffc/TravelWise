using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class CreateAgencyProfileDTO
    {
        [Required]
        public int AgencyApplicationId { get; set; }

        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? OfficeHours { get; set; }
        public string? Languages { get; set; }
        public string? Specializations { get; set; }
        public string? SocialMediaLinks { get; set; }
        public string? TeamMembers { get; set; }
        public string? Certifications { get; set; }
        public string? Awards { get; set; }
        public string? Testimonials { get; set; }
        public string? TermsAndConditions { get; set; }
        public string? PrivacyPolicy { get; set; }
    }

    public class UpdateAgencyProfileDTO
    {
        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? OfficeHours { get; set; }
        public string? Languages { get; set; }
        public string? Specializations { get; set; }
        public string? SocialMediaLinks { get; set; }
        public string? TeamMembers { get; set; }
        public string? Certifications { get; set; }
        public string? Awards { get; set; }
        public string? Testimonials { get; set; }
        public string? TermsAndConditions { get; set; }
        public string? PrivacyPolicy { get; set; }
    }

    public class AgencyProfileResponseDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int AgencyApplicationId { get; set; }
        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? OfficeHours { get; set; }
        public string? Languages { get; set; }
        public string? Specializations { get; set; }
        public string? SocialMediaLinks { get; set; }
        public string? TeamMembers { get; set; }
        public string? Certifications { get; set; }
        public string? Awards { get; set; }
        public string? Testimonials { get; set; }
        public string? TermsAndConditions { get; set; }
        public string? PrivacyPolicy { get; set; }
        public int Rating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalBookings { get; set; }
        public int TotalDeals { get; set; }
        public DateTime? LastActive { get; set; }
        public bool IsOnline { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
