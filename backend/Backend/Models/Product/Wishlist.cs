using System;

namespace Backend.Models.Product
{
    public class Wishlist
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int DealId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Deal Deal { get; set; }
    }
}
