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
    [Route("api/agency-applications")]
    [ApiController]
    public class AgencyApplicationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<AgencyApplicationController> _logger;

        public AgencyApplicationController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IDBHelper dbHelper,
            ILogger<AgencyApplicationController> logger
        )
        {
            _userManager = userManager;
            _context = context;
            _dbHelper = dbHelper;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] AgencyApplicationDTO model)
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

            var application = new AgencyApplication
            {
                UserId = user.Id,
                AgencyName = model.AgencyName,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                Description = model.Description,
                BusinessRegistrationNumber = model.BusinessRegistrationNumber,
            };

            _context.AgencyApplications.Add(application);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Application submitted successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetApplications()
        {
            var applications = await _context
                .AgencyApplications.Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AgencyApplicationResponseDTO
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.User.Email,
                    UserName = a.User.FullName,
                    AgencyName = a.AgencyName,
                    Address = a.Address,
                    PhoneNumber = a.PhoneNumber,
                    Description = a.Description,
                    BusinessRegistrationNumber = a.BusinessRegistrationNumber,
                    CreatedAt = a.CreatedAt,
                    ReviewedAt = a.ReviewedAt,
                    IsApproved = a.IsApproved,
                    RejectionReason = a.RejectionReason,
                    ReviewedBy = a.ReviewedBy,
                })
                .ToListAsync();

            return Ok(applications);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveApplication(int id)
        {
            var application = await _context
                .AgencyApplications.Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound("Application not found");

            if (application.IsApproved)
                return BadRequest("Application is already approved");

            if (application.RejectionReason != null)
                return BadRequest("Application was rejected");

            var user = application.User;
            var result = await _dbHelper.AddAgencyRole(user);

            if (!result)
                return BadRequest("Failed to upgrade user to agency");

            application.IsApproved = true;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedBy = User.Identity.Name;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Application approved successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectApplication(int id, [FromBody] string reason)
        {
            var application = await _context
                .AgencyApplications.Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound("Application not found");

            if (application.IsApproved)
                return BadRequest("Application is already approved");

            if (application.RejectionReason != null)
                return BadRequest("Application was already rejected");

            application.RejectionReason = reason;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedBy = User.Identity.Name;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Application rejected successfully" });
        }

        [Authorize]
        [HttpGet("my-application")]
        public async Task<IActionResult> GetMyApplication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound("User not found");

            var application = await _context
                .AgencyApplications.Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AgencyApplicationResponseDTO
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.User.Email,
                    UserName = a.User.FullName,
                    AgencyName = a.AgencyName,
                    Address = a.Address,
                    PhoneNumber = a.PhoneNumber,
                    Description = a.Description,
                    BusinessRegistrationNumber = a.BusinessRegistrationNumber,
                    CreatedAt = a.CreatedAt,
                    ReviewedAt = a.ReviewedAt,
                    IsApproved = a.IsApproved,
                    RejectionReason = a.RejectionReason,
                    ReviewedBy = a.ReviewedBy,
                })
                .FirstOrDefaultAsync();

            return Ok(application);
        }
    }
}
