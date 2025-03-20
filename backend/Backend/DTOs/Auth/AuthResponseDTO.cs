namespace Backend.DTOs.Auth
{
    public class AuthResponseDTO
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
