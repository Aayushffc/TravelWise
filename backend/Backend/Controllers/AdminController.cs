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
}
