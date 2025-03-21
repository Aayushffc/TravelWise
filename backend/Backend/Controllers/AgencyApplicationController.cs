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
    [Authorize]
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

        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] AgencyApplicationDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(
                        new
                        {
                            message = "Invalid application data",
                            errors = ModelState.Values.SelectMany(v => v.Errors),
                        }
                    );

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                if (!user.EmailConfirmed)
                    return BadRequest(new { message = "Please verify your email before applying" });

                if (await _userManager.IsInRoleAsync(user, "Agency"))
                    return BadRequest(new { message = "You are already registered as an agency" });

                // Check if user already has a pending application
                var existingApplication = await _context.AgencyApplications.FirstOrDefaultAsync(a =>
                    a.UserId == user.Id && !a.IsApproved && a.RejectionReason == null
                );

                if (existingApplication != null)
                    return BadRequest(new { message = "You already have a pending application" });

                var application = new AgencyApplication
                {
                    UserId = user.Id,
                    AgencyName = model.AgencyName,
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber,
                    Description = model.Description,
                    BusinessRegistrationNumber = model.BusinessRegistrationNumber,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.AgencyApplications.Add(application);
                await _context.SaveChangesAsync();

                return Ok(
                    new
                    {
                        message = "Application submitted successfully",
                        applicationId = application.Id,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting agency application");
                return StatusCode(
                    500,
                    new { message = "An error occurred while submitting your application" }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetApplications()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found in GetApplications");
                    return Unauthorized(new { message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"User {user.Email} has roles: {string.Join(", ", roles)}");

                var isAdmin = await _dbHelper.IsUserAdmin(user);
                if (!isAdmin)
                {
                    _logger.LogWarning(
                        $"User {user.Email} attempted to access admin endpoint without Admin role"
                    );
                    return Forbid("Access denied. Admin role required.");
                }

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

                _logger.LogInformation(
                    $"Successfully retrieved {applications.Count} agency applications"
                );
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agency applications");
                return StatusCode(
                    500,
                    new { message = "An error occurred while retrieving applications" }
                );
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveApplication(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var isAdmin = await _dbHelper.IsUserAdmin(user);
                if (!isAdmin)
                {
                    _logger.LogWarning(
                        $"User {user.Email} attempted to approve application without Admin role"
                    );
                    return Forbid("Access denied. Admin role required.");
                }

                var application = await _context
                    .AgencyApplications.Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                    return NotFound(new { message = "Application not found" });

                if (application.IsApproved)
                    return BadRequest(new { message = "Application is already approved" });

                if (application.RejectionReason != null)
                    return BadRequest(new { message = "Application was previously rejected" });

                // Remove User role first
                var removeUserRoleResult = await _userManager.RemoveFromRoleAsync(
                    application.User,
                    "User"
                );
                if (!removeUserRoleResult.Succeeded)
                {
                    _logger.LogError(
                        $"Failed to remove User role: {string.Join(", ", removeUserRoleResult.Errors.Select(e => e.Description))}"
                    );
                    return BadRequest(new { message = "Failed to remove User role" });
                }

                // Add Agency role
                var addAgencyRoleResult = await _dbHelper.AddAgencyRole(application.User);
                if (!addAgencyRoleResult)
                {
                    // Try to restore User role if Agency role assignment fails
                    await _userManager.AddToRoleAsync(application.User, "User");
                    return BadRequest(new { message = "Failed to upgrade user to agency role" });
                }

                application.IsApproved = true;
                application.ReviewedAt = DateTime.UtcNow;
                application.ReviewedBy = User.Identity.Name;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Application approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving agency application {Id}", id);
                return StatusCode(
                    500,
                    new { message = "An error occurred while approving the application" }
                );
            }
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectApplication(
            int id,
            [FromBody] RejectApplicationDTO model
        )
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(
                        new
                        {
                            message = "Invalid rejection data",
                            errors = ModelState.Values.SelectMany(v => v.Errors),
                        }
                    );

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var isAdmin = await _dbHelper.IsUserAdmin(user);
                if (!isAdmin)
                {
                    _logger.LogWarning(
                        $"User {user.Email} attempted to reject application without Admin role"
                    );
                    return Forbid("Access denied. Admin role required.");
                }

                var application = await _context
                    .AgencyApplications.Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                    return NotFound(new { message = "Application not found" });

                if (application.IsApproved)
                    return BadRequest(new { message = "Application is already approved" });

                if (application.RejectionReason != null)
                    return BadRequest(new { message = "Application was already rejected" });

                application.RejectionReason = model.Reason;
                application.ReviewedAt = DateTime.UtcNow;
                application.ReviewedBy = User.Identity.Name;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Application rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting agency application {Id}", id);
                return StatusCode(
                    500,
                    new { message = "An error occurred while rejecting the application" }
                );
            }
        }

        [HttpGet("my-application")]
        public async Task<IActionResult> GetMyApplication()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return NotFound(new { message = "User not found" });

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

                if (application == null)
                    return NotFound(new { message = "No application found" });

                return Ok(application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's agency application");
                return StatusCode(
                    500,
                    new { message = "An error occurred while retrieving your application" }
                );
            }
        }
    }
}
