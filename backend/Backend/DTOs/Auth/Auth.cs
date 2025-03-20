namespace Backend.DTOs.Auth
{
    public class RegisterDTO
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? FullName { get; set; } // Keep for backward compatibility
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class LoginDTO
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class AuthResponseDTO
    {
        public string? Token { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? RefreshToken { get; set; }
    }

    public class EmailVerificationDTO
    {
        public string Email { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class EmailRequestDTO
    {
        public string Email { get; set; } = string.Empty;
    }

    public class RefreshTokenDTO
    {
        public required string RefreshToken { get; set; }
    }
}
