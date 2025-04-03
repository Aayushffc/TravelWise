using Backend.Models.Product;

namespace Backend.Models
{
    public class Location
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Country { get; set; }
        public string? Continent { get; set; }
        public string? Currency { get; set; }
        public int ClickCount { get; set; } = 0;
        public int RequestCallCount { get; set; } = 0;
        public bool IsPopular { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<Deal>? Deals { get; set; }
    }
}
