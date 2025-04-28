using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class CreateReviewDTO
    {
        [Required]
        public int DealId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Text { get; set; }

        public List<string>? Photos { get; set; }
    }

    public class UpdateReviewDTO
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int? Rating { get; set; }

        [MaxLength(1000)]
        public string? Text { get; set; }

        public List<string>? Photos { get; set; }
    }

    public class ReviewResponseDTO
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public string UserId { get; set; }
        public string AgencyId { get; set; }
        public string? UserName { get; set; }
        public string? UserPhoto { get; set; }
        public string? Text { get; set; }
        public List<string>? Photos { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}