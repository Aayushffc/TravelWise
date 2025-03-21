using Backend.DBContext;
using Backend.Models;
using Backend.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            ILogger<AdminController> logger,
            UserManager<ApplicationUser> userManager
        )
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet("agency-applications")]
        public async Task<IActionResult> GetAgencyApplications()
        {
            var applications = await _context
                .AgencyApplications.Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    id = a.Id,
                    userId = a.UserId,
                    userName = a.User.FullName,
                    email = a.User.Email,
                    agencyName = a.AgencyName,
                    businessRegistrationNumber = a.BusinessRegistrationNumber,
                    phoneNumber = a.PhoneNumber,
                    status = a.IsApproved
                        ? "approved"
                        : (a.RejectionReason != null ? "rejected" : "pending"),
                    submittedAt = a.CreatedAt,
                    reviewedAt = a.ReviewedAt,
                    reviewedBy = a.ReviewedBy,
                })
                .ToListAsync();

            return Ok(applications);
        }

        [HttpPost("agency-applications/{id}/approve")]
        public async Task<IActionResult> ApproveAgencyApplication(int id)
        {
            var application = await _context
                .AgencyApplications.Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound();

            if (application.IsApproved)
                return BadRequest("Application is already approved");

            if (application.RejectionReason != null)
                return BadRequest("Application was rejected");

            // Add user to Agency role
            var roleResult = await _userManager.AddToRoleAsync(application.User, "Agency");
            if (!roleResult.Succeeded)
                return BadRequest("Failed to assign agency role");

            application.IsApproved = true;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedBy = User.Identity.Name;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Agency application approved successfully" });
        }

        [HttpPost("agency-applications/{id}/reject")]
        public async Task<IActionResult> RejectAgencyApplication(
            int id,
            [FromBody] RejectAgencyDTO model
        )
        {
            var application = await _context
                .AgencyApplications.Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound();

            if (application.IsApproved)
                return BadRequest("Cannot reject an approved application");

            application.RejectionReason = model.Reason;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedBy = User.Identity.Name;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Agency application rejected successfully" });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager
                .Users.Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    fullName = u.FullName,
                    emailConfirmed = u.EmailConfirmed,
                    phoneNumber = u.PhoneNumber,
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("faqs")]
        public async Task<IActionResult> GetFAQs()
        {
            var faqs = await _context
                .FAQs.OrderBy(f => f.Category)
                .ThenBy(f => f.OrderIndex)
                .Select(f => new
                {
                    id = f.Id,
                    question = f.Question,
                    answer = f.Answer,
                    category = f.Category,
                    isActive = f.IsActive,
                    createdAt = f.CreatedAt,
                })
                .ToListAsync();

            return Ok(faqs);
        }

        [HttpPost("faqs/{id}/toggle-status")]
        public async Task<IActionResult> ToggleFAQStatus(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);

            if (faq == null)
                return NotFound();

            faq.IsActive = !faq.IsActive;
            await _context.SaveChangesAsync();

            return Ok(
                new { message = $"FAQ {(faq.IsActive ? "activated" : "deactivated")} successfully" }
            );
        }
    }

    public class RejectAgencyDTO
    {
        public string Reason { get; set; }
    }
}
