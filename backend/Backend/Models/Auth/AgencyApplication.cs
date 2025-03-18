namespace Backend.Models.Auth
{
    public class AgencyApplication
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string? AgencyName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Description { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
        public string? ReviewedBy { get; set; }
    }
}
