using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using Backend.DBContext;
using Backend.DTOs.Auth;
using Backend.Helper;
using Backend.Models.Auth;
using Google.Apis.Auth;
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

            // Check if user exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // Check if user was registered through Google
                if (existingUser.PasswordHash == null)
                {
                    return BadRequest(
                        new
                        {
                            message = "This email is already registered through Google. Please use Google login instead.",
                        }
                    );
                }
                return BadRequest(
                    new { message = "Email already exists. Please use login instead." }
                );
            }

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
            var user = users.First();

            // Check if user was registered through Google (no password)
            if (user.PasswordHash == null)
            {
                return BadRequest(
                    new
                    {
                        message = "This account was registered through Google. Please use Google login instead.",
                    }
                );
            }

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
                return StatusCode(
                    500,
                    "Failed to send verification email. Please try again later."
                );
            }
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> RequestVerificationCode([FromBody] GoogleLoginDTO model)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    model.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _configuration["Authentication:Google:ClientId"] },
                    }
                );

                var user = await _userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    // Register a new user
                    user = new ApplicationUser
                    {
                        Email = payload.Email,
                        UserName = payload.Email,
                        FirstName = payload.GivenName ?? "Google",
                        LastName = payload.FamilyName ?? "User",
                        FullName = payload.Name,
                        EmailConfirmed = true,
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                        return BadRequest(result.Errors);

                    await _dbHelper.AddUserRole(user); // Add default role
                }
                else if (user.PasswordHash != null)
                {
                    // User exists but was registered through regular registration
                    return BadRequest(
                        new
                        {
                            message = "This email is already registered. Please use regular login instead.",
                        }
                    );
                }

                var token = await _dbHelper.GenerateJwtToken(user);

                return Ok(
                    new
                    {
                        token,
                        user = new
                        {
                            user.Id,
                            user.Email,
                            user.FullName,
                            user.FirstName,
                            user.LastName,
                            user.EmailConfirmed,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                return Unauthorized("Google token validation failed.");
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
