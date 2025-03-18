namespace Backend.DTOs.Auth
{
    public class EmailVerificationDTO
    {
        public required string Email { get; set; }
        public required string VerificationCode { get; set; }
    }
}