using Backend.DTOs;
using Backend.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DealController : ControllerBase
    {
        private readonly IDBHelper _dbHelper;

        public DealController(IDBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // GET: api/Deal
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DealResponseDto>>> GetDeals(
            [FromQuery] int? locationId = null
        )
        {
            var deals = await _dbHelper.GetDeals(locationId);
            return Ok(deals);
        }

        // GET: api/Deal/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DealResponseDto>> GetDeal(int id)
        {
            var deal = await _dbHelper.GetDealById(id);

            if (deal == null)
            {
                return NotFound();
            }

            return deal;
        }

        // POST: api/Deal
        [HttpPost]
        public async Task<ActionResult<DealResponseDto>> CreateDeal(DealCreateDto dealDto)
        {
            // Get the current user's ID from the User.Identity
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            dealDto.UserId = userId;
            var deal = await _dbHelper.CreateDeal(dealDto);
            return CreatedAtAction(nameof(GetDeal), new { id = deal.Id }, deal);
        }

        // PUT: api/Deal/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDeal(int id, DealUpdateDto dealDto)
        {
            var success = await _dbHelper.UpdateDeal(id, dealDto);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Deal/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeal(int id)
        {
            var success = await _dbHelper.DeleteDeal(id);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // GET: api/Deal/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<DealResponseDto>>> SearchDeals(
            [FromQuery] string? searchTerm,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int? minDays,
            [FromQuery] int? maxDays,
            [FromQuery] string? packageType
        )
        {
            var deals = await _dbHelper.SearchDeals(
                searchTerm,
                minPrice,
                maxPrice,
                minDays,
                maxDays,
                packageType
            );

            return Ok(deals);
        }
    }
}
