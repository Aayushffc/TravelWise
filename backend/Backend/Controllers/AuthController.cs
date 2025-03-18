using System.Security.Claims;
using System.Security.Cryptography;
using Backend.DBContext;
using Backend.DTOs.Auth;
using Backend.Helper;
using Backend.Models.Auth;
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
                FullName = model.FullName,
                EmailConfirmed = false,
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _dbHelper.AddUserRole(user);

            // Generate and send verification code
            var verificationCode = GenerateVerificationCode();
            await _emailService.SendVerificationEmailAsync(user, verificationCode);

            // Store verification code in user claims
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

            return Ok(
                new
                {
                    message = "User registered successfully. Please check your email for verification code.",
                }
            );
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
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

        [Authorize]
        [HttpPost("request-agency-upgrade")]
        public async Task<IActionResult> RequestAgencyUpgrade()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound("User not found");

            if (!user.EmailConfirmed)
                return BadRequest("Please verify your email first");

            if (await _userManager.IsInRoleAsync(user, "Agency"))
                return BadRequest("User is already an agency");

            // Check if user already has a pending application
            var existingApplication = await _context.AgencyApplications.FirstOrDefaultAsync(a =>
                a.UserId == user.Id && !a.IsApproved && a.RejectionReason == null
            );

            if (existingApplication != null)
                return BadRequest("You already have a pending application");

            return Ok(
                new { message = "Please submit your agency application with required details" }
            );
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized("Invalid credentials");

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                false,
                false
            );
            if (!result.Succeeded)
                return Unauthorized("Invalid credentials");

            var token = await _dbHelper.GenerateJwtToken(user);
            return Ok(new AuthResponseDTO { Token = token, Email = user.Email });
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                "Google",
                "/api/auth/google-callback"
            );
            return Challenge(properties, "Google");
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return BadRequest("Error loading external login information.");

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false
            );
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(
                    info.Principal.FindFirstValue(ClaimTypes.Email)
                );
                var token = await _dbHelper.GenerateJwtToken(user);

                // Redirect to frontend with token
                return Redirect($"{_configuration["FrontendUrl"]}/auth/callback?token={token}");
            }

            return BadRequest("Failed to authenticate with Google.");
        }

        private string GenerateVerificationCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var codeBytes = new byte[3];
            rng.GetBytes(codeBytes);
            return Convert
                .ToBase64String(codeBytes)
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 6);
        }
    }
}
