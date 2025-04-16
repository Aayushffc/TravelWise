using System.ComponentModel.DataAnnotations;

public class UpdateProfileDTO
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class ForgotPasswordDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}

public class ResetPasswordDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Token { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; }
}
public class GoogleLoginDTO
{
    public string IdToken { get; set; } = default!;
}
