using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class FAQResponseDTO
    {
        public int Id { get; set; }
        public string? Question { get; set; }
        public string? Answer { get; set; }
        public int OrderIndex { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class FAQCreateDTO
    {
        [Required]
        public string? Question { get; set; }
        public string? Category { get; set; }
    }

    public class FAQUpdateDTO
    {
        [Required]
        public string? Answer { get; set; }
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; }
    }

    public class FAQSearchDTO
    {
        [Required]
        public string? Query { get; set; }
    }
}
