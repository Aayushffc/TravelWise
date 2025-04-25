using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class FAQ
    {
        public int Id { get; set; }

        [Required]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }

        public int OrderIndex { get; set; }

        public string Category { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
