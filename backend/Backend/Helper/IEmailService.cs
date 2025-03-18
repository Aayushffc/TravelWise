using Backend.Models.Auth;

namespace Backend.Helper
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendVerificationEmailAsync(ApplicationUser user, string verificationCode);
        Task SendAgencyUpgradeEmailAsync(ApplicationUser user, string verificationCode);
    }
}
