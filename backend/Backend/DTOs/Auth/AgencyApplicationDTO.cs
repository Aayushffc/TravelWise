using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth
{
    public class AgencyApplicationDTO
    {
        [Required]
        [MaxLength(200)]
        public string? AgencyName { get; set; }

        [Required]
        [MaxLength(500)]
        public string? Address { get; set; }

        [Required]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string? BusinessRegistrationNumber { get; set; }
    }

    public class AgencyApplicationResponseDTO
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string? AgencyName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Description { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
        public string? ReviewedBy { get; set; }
    }
}
