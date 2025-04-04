using System.ComponentModel.DataAnnotations;
using Backend.Models.Auth;

namespace Backend.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        
        [Required]
        public int BookingId { get; set; }
        public Booking Booking { get; set; }
        
        [Required]
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }
        
        [Required]
        public string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public bool IsRead { get; set; }
        
        public string? MessageType { get; set; } // text, image, file, etc.
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }
} 