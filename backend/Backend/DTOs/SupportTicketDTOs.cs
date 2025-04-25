using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class SupportTicketResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string ProblemTitle { get; set; }
        public string ProblemDescription { get; set; }
        public string Status { get; set; }
        public string AdminResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class SupportTicketCreateDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string ProblemTitle { get; set; }

        [Required]
        public string ProblemDescription { get; set; }
    }

    public class SupportTicketUpdateDTO
    {
        [Required]
        public string AdminResponse { get; set; }
    }

    public class SupportTicketStatusDTO
    {
        [Required]
        public string Status { get; set; }
    }
}
