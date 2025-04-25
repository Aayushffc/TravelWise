using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string ProblemTitle { get; set; }

        [Required]
        public string ProblemDescription { get; set; }

        public string Status { get; set; } = "Open"; // Open, In Progress, Resolved, Closed

        public string? AdminResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
