using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Backend.DTOs
{
    public class SocialMediaLinkDTO
    {
        public string Platform { get; set; }
        public string Url { get; set; }
    }

    public class TeamMemberDTO
    {
        public string Name { get; set; }
        public string Role { get; set; }
    }

    public class CertificationDTO
    {
        public string Name { get; set; }
        public string Provider { get; set; }
        public DateTime Date { get; set; }
    }

    public class AwardDTO
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
    }

    public class TestimonialDTO
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }

    public class CreateAgencyProfileDTO
    {
        public string Website { get; set; }
        public string Email { get; set; }
        public string LogoUrl { get; set; }
        public string CoverImageUrl { get; set; }
        public string OfficeHours { get; set; }
        public List<string> Languages { get; set; }
        public string Specializations { get; set; }
        public List<SocialMediaLinkDTO> SocialMediaLinks { get; set; }
        public List<TeamMemberDTO> TeamMembers { get; set; }
        public List<CertificationDTO> Certifications { get; set; }
        public List<AwardDTO> Awards { get; set; }
        public List<TestimonialDTO> Testimonials { get; set; }
        public string TermsAndConditions { get; set; }
        public string PrivacyPolicy { get; set; }
    }

    public class UpdateAgencyProfileDTO
    {
        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? OfficeHours { get; set; }
        public List<string>? Languages { get; set; }
        public string? Specializations { get; set; }
        public List<SocialMediaLinkDTO>? SocialMediaLinks { get; set; }
        public List<TeamMemberDTO>? TeamMembers { get; set; }
        public List<CertificationDTO>? Certifications { get; set; }
        public List<AwardDTO>? Awards { get; set; }
        public List<TestimonialDTO>? Testimonials { get; set; }
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
        public List<string>? Languages { get; set; }
        public string? Specializations { get; set; }
        public List<SocialMediaLinkDTO>? SocialMediaLinks { get; set; }
        public List<TeamMemberDTO>? TeamMembers { get; set; }
        public List<CertificationDTO>? Certifications { get; set; }
        public List<AwardDTO>? Awards { get; set; }
        public List<TestimonialDTO>? Testimonials { get; set; }
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

        // Agency Application Fields
        public string? AgencyName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Description { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public DateTime ApplicationCreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
        public string? ReviewedBy { get; set; }
    }

    public class AgencyInfoResponseDTO
    {
        public int AgencyId { get; set; }
        public string AgencyName { get; set; }
        public string LogoUrl { get; set; }
        public int Rating { get; set; }
        public string PhoneNumber { get; set; }
        public string Website { get; set; }
        public int TotalReviews { get; set; }
        public List<string> Languages { get; set; }
    }
}
