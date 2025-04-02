using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using Backend.DBContext;
using Backend.DTOs.Auth;
using Backend.Helper;
using Backend.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TravelWiseAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IDBHelper _dbHelper;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly ApplicationDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IDBHelper dbHelper,
            IEmailService emailService,
            ILogger<AuthController> logger,
            ApplicationDbContext context
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dbHelper = dbHelper;
            _emailService = emailService;
            _logger = logger;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                FullName = $"{model.FirstName} {model.LastName}",
                EmailConfirmed = false, // User starts with unverified email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _dbHelper.AddUserRole(user);

            // Generate a token for the user
            var token = await _dbHelper.GenerateJwtToken(user);

            return Ok(
                new
                {
                    message = "User registered successfully.",
                    token = token,
                    user = new
                    {
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        fullName = user.FullName,
                        emailConfirmed = user.EmailConfirmed,
                    },
                }
            );
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationDTO model)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
                return NotFound("User not found");

            var verificationClaim = await _userManager.GetClaimsAsync(user);
            var codeClaim = verificationClaim.FirstOrDefault(c => c.Type == "VerificationCode");
            var expiryClaim = verificationClaim.FirstOrDefault(c =>
                c.Type == "VerificationCodeExpiry"
            );

            if (codeClaim == null || expiryClaim == null)
                return BadRequest("No verification code found. Please request a new one.");

            if (DateTime.Parse(expiryClaim.Value) < DateTime.UtcNow)
                return BadRequest("Verification code has expired. Please request a new one.");

            if (codeClaim.Value != model.VerificationCode)
                return BadRequest("Invalid verification code");

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            // Remove verification claims
            await _userManager.RemoveClaimAsync(user, codeClaim);
            await _userManager.RemoveClaimAsync(user, expiryClaim);

            return Ok(new { message = "Email verified successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            // Instead of FindByEmailAsync which fails on duplicate emails, use a manual query
            var users = await _userManager.Users.Where(u => u.Email == model.Email).ToListAsync();
            if (users.Count == 0)
                return Unauthorized("Invalid credentials");

            // If multiple users found with the same email, use the first one
            // This is a temporary fix - you should ensure email uniqueness in your database
            var user = users.First();

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                false,
                false
            );
            if (!result.Succeeded)
                return Unauthorized("Invalid credentials");

            var token = await _dbHelper.GenerateJwtToken(user);
            return Ok(
                new AuthResponseDTO
                {
                    Id = user.Id,
                    Token = token,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                }
            );
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var returnUrl = _configuration["FrontendUrl"] + "/auth/callback";
            var properties = new AuthenticationProperties { RedirectUri = returnUrl };
            return Challenge(properties, "Google");
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback(string returnUrl)
        {
            try
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    _logger.LogError("Failed to get external login info");
                    return Redirect(
                        $"{_configuration["FrontendUrl"]}/auth/callback?error=external_login_failed"
                    );
                }

                _logger.LogInformation(
                    "External login info received for email: {Email}",
                    info.Principal.FindFirstValue(ClaimTypes.Email)
                );

                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogError("Failed to get email from Google login");
                    return BadRequest("Could not retrieve email from Google login.");
                }

                // Try to sign in with existing account
                var result = await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider,
                    info.ProviderKey,
                    isPersistent: false
                );

                ApplicationUser user;

                if (result.Succeeded)
                {
                    // Existing user login
                    user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (user == null)
                    {
                        // If somehow FirstOrDefaultAsync returned null but ExternalLoginSignInAsync succeeded,
                        // we'll just create a new user
                        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
                        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";
                        var fullName = $"{firstName} {lastName}".Trim();

                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FirstName = firstName,
                            LastName = lastName,
                            FullName = fullName,
                            EmailConfirmed = true,
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            _logger.LogError(
                                "Failed to create user from Google login: {Errors}",
                                string.Join(", ", createResult.Errors.Select(e => e.Description))
                            );
                            return BadRequest("Failed to create user from Google login.");
                        }

                        await _dbHelper.AddUserRole(user);
                    }
                }
                else
                {
                    // New user, create an account
                    var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
                    var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";
                    var fullName = $"{firstName} {lastName}".Trim();

                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        FullName = fullName,
                        EmailConfirmed = true,
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        _logger.LogError(
                            "Failed to create user from Google login: {Errors}",
                            string.Join(", ", createResult.Errors.Select(e => e.Description))
                        );
                        return BadRequest("Failed to create user from Google login.");
                    }

                    // Add user to default role
                    await _dbHelper.AddUserRole(user);

                    // Add the external login provider to the user
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (!addLoginResult.Succeeded)
                    {
                        _logger.LogError(
                            "Failed to add Google login information: {Errors}",
                            string.Join(", ", addLoginResult.Errors.Select(e => e.Description))
                        );
                        return BadRequest("Failed to add Google login information to user.");
                    }
                }

                // Generate token
                var token = await _dbHelper.GenerateJwtToken(user);

                // Set the token as a cookie
                Response.Cookies.Append(
                    "ACCESS_TOKEN",
                    token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddDays(7),
                    }
                );

                return Redirect($"{returnUrl}?success=true");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google callback");
                return Redirect($"{returnUrl}?error=authentication_failed");
            }
        }

        [HttpPost("request-verification-code")]
        public async Task<IActionResult> RequestVerificationCode([FromBody] EmailRequestDTO model)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
                return NotFound("User not found");

            if (user.EmailConfirmed)
                return BadRequest("Email is already verified");

            try
            {
                // Generate and send verification code
                var verificationCode = GenerateVerificationCode();
                await _emailService.SendVerificationEmailAsync(user, verificationCode);

                // Remove any existing verification claims
                var existingClaims = await _userManager.GetClaimsAsync(user);
                var codeClaim = existingClaims.FirstOrDefault(c => c.Type == "VerificationCode");
                var expiryClaim = existingClaims.FirstOrDefault(c =>
                    c.Type == "VerificationCodeExpiry"
                );

                if (codeClaim != null)
                    await _userManager.RemoveClaimAsync(user, codeClaim);
                if (expiryClaim != null)
                    await _userManager.RemoveClaimAsync(user, expiryClaim);

                // Store new verification code in user claims
                await _userManager.AddClaimAsync(
                    user,
                    new System.Security.Claims.Claim("VerificationCode", verificationCode)
                );
                await _userManager.AddClaimAsync(
                    user,
                    new System.Security.Claims.Claim(
                        "VerificationCodeExpiry",
                        DateTime.UtcNow.AddMinutes(10).ToString("O")
                    )
                );

                return Ok(new { message = "Verification code sent to your email." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email to {Email}", user.Email);
                return StatusCode(
                    500,
                    "Failed to send verification email. Please try again later."
                );
            }
        }

        private string GenerateVerificationCode()
        {
            try
            {
                using var rng = RandomNumberGenerator.Create();
                var codeBytes = new byte[4]; // Adjusted size
                rng.GetBytes(codeBytes);
                return Convert
                    .ToBase64String(codeBytes)
                    .Replace("/", "_")
                    .Replace("+", "-")
                    .Substring(0, 6);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating verification code");
                throw;
            }
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound("User not found");

            return Ok(
                new
                {
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    fullName = user.FullName,
                    emailConfirmed = user.EmailConfirmed,
                    phoneNumber = user.PhoneNumber,
                }
            );
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound("User not found");

            if (!string.IsNullOrEmpty(model.FullName))
                user.FullName = model.FullName;

            if (!string.IsNullOrEmpty(model.PhoneNumber))
                user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Ok(
                    new { message = "If the email exists, a password reset link will be sent." }
                );

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Create reset password link
            var resetLink =
                $"{_configuration["FrontendUrl"]}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            try
            {
                // Send email with reset link
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
                return Ok(
                    new { message = "If the email exists, a password reset link will be sent." }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", user.Email);
                return StatusCode(
                    500,
                    "Failed to send password reset email. Please try again later."
                );
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest("Invalid request");

            var result = await _userManager.ResetPasswordAsync(
                user,
                model.Token,
                model.NewPassword
            );
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Password has been reset successfully" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Clear the authentication cookie
            await _signInManager.SignOutAsync();

            // Clear the JWT token cookie
            Response.Cookies.Delete(
                "ACCESS_TOKEN",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                }
            );

            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound("User not found");

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword
            );
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Password changed successfully" });
        }

        [Authorize]
        [HttpGet("role")]
        public async Task<IActionResult> GetUserRole()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User"; // Default to "User" if no role is assigned

            return Ok(new { role = role });
        }
    }
}
