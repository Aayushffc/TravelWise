using System.Net;
using System.Net.Mail;
using Backend.Models.Auth;
using Microsoft.Extensions.Configuration;

namespace Backend.Helper
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            message.To.Add(to);
            await client.SendMailAsync(message);
        }

        public async Task SendVerificationEmailAsync(ApplicationUser user, string verificationCode)
        {
            var subject = "Verify your TravelWise email";
            var body =
                $@"
                <h2>Welcome to TravelWise!</h2>
                <p>Dear {user.FullName},</p>
                <p>Thank you for registering with TravelWise. Please use the following verification code to verify your email:</p>
                <h3 style='color: #007bff; font-size: 24px;'>{verificationCode}</h3>
                <p>This code will expire in 10 minutes.</p>
                <p>If you didn't request this verification, please ignore this email.</p>
            ";

            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendAgencyUpgradeEmailAsync(ApplicationUser user, string verificationCode)
        {
            var subject = "Verify your TravelWise Agency Upgrade";
            var body =
                $@"
                <h2>Agency Upgrade Verification</h2>
                <p>Dear {user.FullName},</p>
                <p>You have requested to upgrade your account to an Agency account. Please use the following verification code to complete the upgrade:</p>
                <h3 style='color: #007bff; font-size: 24px;'>{verificationCode}</h3>
                <p>This code will expire in 10 minutes.</p>
                <p>If you didn't request this upgrade, please ignore this email.</p>
            ";

            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var subject = "Reset Your Password";
            var body =
                $@"
                <h2>Reset Your Password</h2>
                <p>Click the link below to reset your password:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>If you didn't request this, please ignore this email.</p>";

            await SendEmailAsync(email, subject, body);
        }
    }
}
