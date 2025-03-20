namespace Backend.DTOs.Auth
{
    public class EmailVerificationDTO
    {
        public string? Email { get; set; }
        public string? VerificationCode { get; set; }
    }
}
