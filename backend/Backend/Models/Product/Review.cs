using Backend.Models.Auth;

namespace Backend.Models.Product
{
    public class Review
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public virtual Deal? Deal { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public string AgencyId { get; set; }
        public virtual ApplicationUser? Agency { get; set; }
        public string? Text { get; set; }
        public string? Photos { get; set; } // Store as JSON array
        public int Rating { get; set; } // 1-5
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}