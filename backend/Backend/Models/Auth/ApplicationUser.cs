using Microsoft.AspNetCore.Identity;

namespace Backend.Models.Auth
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty; // Keep for backward compatibility
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }

        // Helper method to get full name
        public string GetFullName()
        {
            return $"{FirstName} {LastName}".Trim();
        }

        // Update FullName based on FirstName and LastName
        public void UpdateFullName()
        {
            FullName = GetFullName();
        }
    }
}
