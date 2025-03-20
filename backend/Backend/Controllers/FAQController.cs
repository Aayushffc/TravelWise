using Backend.DBContext;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FAQController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FAQController> _logger;

        public FAQController(ApplicationDbContext context, ILogger<FAQController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FAQ>>> GetFAQs()
        {
            try
            {
                var faqs = await _context.FAQs
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.Category)
                    .ThenBy(f => f.OrderIndex)
                    .ToListAsync();

                // Group FAQs by category
                var groupedFaqs = faqs
                    .GroupBy(f => f.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Questions = g.Select(q => new
                        {
                            q.Id,
                            q.Question,
                            q.Answer,
                            q.OrderIndex
                        })
                    });

                return Ok(groupedFaqs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving FAQs");
                return StatusCode(500, "An error occurred while retrieving FAQs");
            }
        }
    }
}