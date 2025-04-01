using System.Security.Claims;
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
        private readonly ILogger<DealController> _logger;

        public DealController(IDBHelper dbHelper, ILogger<DealController> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        // GET: api/Deal
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DealResponseDto>>> GetDeals(
            [FromQuery] int? locationId = null
        )
        {
            try
            {
                var deals = await _dbHelper.GetDeals(locationId);
                return Ok(deals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deals");
                return StatusCode(500, "An error occurred while retrieving deals");
            }
        }

        // GET: api/Deal/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DealResponseDto>> GetDeal(int id)
        {
            try
            {
                var deal = await _dbHelper.GetDealById(id);

                if (deal == null)
                {
                    return NotFound($"Deal with ID {id} not found");
                }

                await _dbHelper.IncrementDealClickCount(id);
                return deal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deal {DealId}", id);
                return StatusCode(500, "An error occurred while retrieving the deal");
            }
        }

        // POST: api/Deal
        [HttpPost]
        public async Task<ActionResult<DealResponseDto>> CreateDeal(DealCreateDto dealDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get the current user's ID from the User.Identity
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Validate required fields
                if (
                    string.IsNullOrEmpty(dealDto.Title)
                    || dealDto.LocationId <= 0
                    || dealDto.Price <= 0
                )
                {
                    return BadRequest("Required fields are missing or invalid");
                }

                dealDto.UserId = userId;
                var deal = await _dbHelper.CreateDeal(dealDto);

                if (deal == null)
                {
                    return StatusCode(500, "Failed to create deal");
                }

                return CreatedAtAction(nameof(GetDeal), new { id = deal.Id }, deal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deal");
                return StatusCode(500, "An error occurred while creating the deal");
            }
        }

        // PUT: api/Deal/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDeal(int id, DealUpdateDto dealDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get the current user's ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Check if the deal exists and belongs to the user
                var existingDeal = await _dbHelper.GetDealById(id);
                if (existingDeal == null)
                {
                    return NotFound($"Deal with ID {id} not found");
                }

                if (existingDeal.UserId != userId)
                {
                    return StatusCode(403, "You don't have permission to update this deal");
                }

                var success = await _dbHelper.UpdateDeal(id, dealDto);

                if (!success)
                {
                    return StatusCode(500, "Failed to update deal");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deal {DealId}", id);
                return StatusCode(500, "An error occurred while updating the deal");
            }
        }

        // DELETE: api/Deal/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeal(int id)
        {
            try
            {
                // Get the current user's ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Check if the deal exists and belongs to the user
                var existingDeal = await _dbHelper.GetDealById(id);
                if (existingDeal == null)
                {
                    return NotFound($"Deal with ID {id} not found");
                }

                if (existingDeal.UserId != userId)
                {
                    return Forbid("You don't have permission to delete this deal");
                }

                var success = await _dbHelper.DeleteDeal(id);

                if (!success)
                {
                    return StatusCode(500, "Failed to delete deal");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deal {DealId}", id);
                return StatusCode(500, "An error occurred while deleting the deal");
            }
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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching deals");
                return StatusCode(500, "An error occurred while searching deals");
            }
        }

        // GET: api/Deal/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<DealResponseDto>>> GetDealsByUserId(
            string userId
        )
        {
            try
            {
                var deals = await _dbHelper.GetDealsByUserId(userId);
                return Ok(deals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deals for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving deals");
            }
        }
    }
}
