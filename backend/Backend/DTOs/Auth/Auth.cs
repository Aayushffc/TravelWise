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

    public class EmailRequestDTO
    {
        public string Email { get; set; } = string.Empty;
    }
}
