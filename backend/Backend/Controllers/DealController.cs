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
            [FromQuery] int? locationId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isFeatured = null,
            [FromQuery] string? packageType = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int? minDays = null,
            [FromQuery] int? maxDays = null
        )
        {
            try
            {
                var deals = await _dbHelper.GetDeals(
                    locationId,
                    null,
                    isActive,
                    isFeatured,
                    packageType,
                    minPrice,
                    maxPrice,
                    minDays,
                    maxDays
                );

                if (deals == null || !deals.Any())
                {
                    return Ok(new List<DealResponseDto>());
                }

                return Ok(deals);
            }
            catch (Exception ex)
            {
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
                    || dealDto.DaysCount <= 0
                    || dealDto.NightsCount <= 0
                    || string.IsNullOrEmpty(dealDto.Description)
                    || dealDto.Photos == null
                    || !dealDto.Photos.Any()
                    || dealDto.Itinerary == null
                    || !dealDto.Itinerary.Any()
                    || string.IsNullOrEmpty(dealDto.PackageType)
                )
                {
                    return BadRequest("Required fields are missing or invalid");
                }

                // Set default values
                dealDto.UserId = userId;
                dealDto.CreatedAt = DateTime.UtcNow;
                dealDto.UpdatedAt = DateTime.UtcNow;

                var deal = await _dbHelper.CreateDeal(dealDto);
                if (deal == null)
                {
                    return StatusCode(500, "Failed to create deal");
                }

                // Update total deals count
                await _dbHelper.UpdateAgencyTotalDeals(userId, 1);

                return CreatedAtAction(nameof(GetDeal), new { id = deal.Id }, deal);
            }
            catch (Exception ex)
            {
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

                // Update all properties from the DTO
                existingDeal.Title = dealDto.Title ?? existingDeal.Title;
                existingDeal.Description = dealDto.Description ?? existingDeal.Description;
                existingDeal.LocationId = dealDto.LocationId ?? existingDeal.LocationId;
                existingDeal.Price = dealDto.Price ?? existingDeal.Price;
                existingDeal.DiscountPercentage =
                    dealDto.DiscountPercentage ?? existingDeal.DiscountPercentage;
                existingDeal.DiscountedPrice =
                    dealDto.DiscountedPrice ?? existingDeal.DiscountedPrice;
                existingDeal.DaysCount = dealDto.DaysCount ?? existingDeal.DaysCount;
                existingDeal.NightsCount = dealDto.NightsCount ?? existingDeal.NightsCount;
                existingDeal.PackageType = dealDto.PackageType ?? existingDeal.PackageType;
                existingDeal.Headlines = dealDto.Headlines ?? existingDeal.Headlines;
                existingDeal.Tags = dealDto.Tags ?? existingDeal.Tags;
                existingDeal.Seasons = dealDto.Seasons ?? existingDeal.Seasons;
                existingDeal.Languages = dealDto.Languages ?? existingDeal.Languages;
                existingDeal.Requirements = dealDto.Requirements ?? existingDeal.Requirements;
                existingDeal.Restrictions = dealDto.Restrictions ?? existingDeal.Restrictions;
                existingDeal.IsActive = dealDto.IsActive ?? existingDeal.IsActive;
                existingDeal.IsInstantBooking =
                    dealDto.IsInstantBooking ?? existingDeal.IsInstantBooking;
                existingDeal.IsLastMinuteDeal =
                    dealDto.IsLastMinuteDeal ?? existingDeal.IsLastMinuteDeal;
                existingDeal.MinGroupSize = dealDto.MinGroupSize ?? existingDeal.MinGroupSize;
                existingDeal.MaxGroupSize = dealDto.MaxGroupSize ?? existingDeal.MaxGroupSize;
                existingDeal.DifficultyLevel =
                    dealDto.DifficultyLevel ?? existingDeal.DifficultyLevel;
                existingDeal.ValidFrom = dealDto.ValidFrom ?? existingDeal.ValidFrom;
                existingDeal.ValidUntil = dealDto.ValidUntil ?? existingDeal.ValidUntil;
                existingDeal.MapUrl = dealDto.MapUrl ?? existingDeal.MapUrl;

                // Update nested objects and arrays
                if (dealDto.PackageOptions != null)
                {
                    existingDeal.PackageOptions = dealDto.PackageOptions;
                }
                if (dealDto.Itinerary != null)
                {
                    existingDeal.Itinerary = dealDto.Itinerary;
                }
                if (dealDto.Policies != null)
                {
                    existingDeal.Policies = dealDto.Policies;
                }
                if (dealDto.Photos != null)
                {
                    existingDeal.Photos = dealDto.Photos;
                }

                // Update features
                existingDeal.ElderlyFriendly =
                    dealDto.ElderlyFriendly ?? existingDeal.ElderlyFriendly;
                existingDeal.InternetIncluded =
                    dealDto.InternetIncluded ?? existingDeal.InternetIncluded;
                existingDeal.TravelIncluded = dealDto.TravelIncluded ?? existingDeal.TravelIncluded;
                existingDeal.MealsIncluded = dealDto.MealsIncluded ?? existingDeal.MealsIncluded;
                existingDeal.SightseeingIncluded =
                    dealDto.SightseeingIncluded ?? existingDeal.SightseeingIncluded;
                existingDeal.StayIncluded = dealDto.StayIncluded ?? existingDeal.StayIncluded;
                existingDeal.AirTransfer = dealDto.AirTransfer ?? existingDeal.AirTransfer;
                existingDeal.RoadTransfer = dealDto.RoadTransfer ?? existingDeal.RoadTransfer;
                existingDeal.TrainTransfer = dealDto.TrainTransfer ?? existingDeal.TrainTransfer;
                existingDeal.TravelCostIncluded =
                    dealDto.TravelCostIncluded ?? existingDeal.TravelCostIncluded;
                existingDeal.GuideIncluded = dealDto.GuideIncluded ?? existingDeal.GuideIncluded;
                existingDeal.PhotographyIncluded =
                    dealDto.PhotographyIncluded ?? existingDeal.PhotographyIncluded;
                existingDeal.InsuranceIncluded =
                    dealDto.InsuranceIncluded ?? existingDeal.InsuranceIncluded;
                existingDeal.VisaIncluded = dealDto.VisaIncluded ?? existingDeal.VisaIncluded;

                // Set updated timestamp
                existingDeal.UpdatedAt = DateTime.UtcNow;

                // Convert to DealUpdateDto
                var updateDto = new DealUpdateDto
                {
                    Title = existingDeal.Title,
                    Description = existingDeal.Description,
                    LocationId = existingDeal.LocationId,
                    Price = existingDeal.Price,
                    DiscountPercentage = existingDeal.DiscountPercentage,
                    DiscountedPrice = existingDeal.DiscountedPrice,
                    DaysCount = existingDeal.DaysCount,
                    NightsCount = existingDeal.NightsCount,
                    PackageType = existingDeal.PackageType,
                    Headlines = existingDeal.Headlines,
                    Tags = existingDeal.Tags,
                    Seasons = existingDeal.Seasons,
                    Languages = existingDeal.Languages,
                    Requirements = existingDeal.Requirements,
                    Restrictions = existingDeal.Restrictions,
                    IsActive = existingDeal.IsActive,
                    IsInstantBooking = existingDeal.IsInstantBooking,
                    IsLastMinuteDeal = existingDeal.IsLastMinuteDeal,
                    MinGroupSize = existingDeal.MinGroupSize,
                    MaxGroupSize = existingDeal.MaxGroupSize,
                    DifficultyLevel = existingDeal.DifficultyLevel,
                    ValidFrom = existingDeal.ValidFrom,
                    ValidUntil = existingDeal.ValidUntil,
                    MapUrl = existingDeal.MapUrl,
                    PackageOptions = existingDeal.PackageOptions,
                    Itinerary = existingDeal.Itinerary,
                    Policies = existingDeal.Policies,
                    Photos = existingDeal.Photos,
                    ElderlyFriendly = existingDeal.ElderlyFriendly,
                    InternetIncluded = existingDeal.InternetIncluded,
                    TravelIncluded = existingDeal.TravelIncluded,
                    MealsIncluded = existingDeal.MealsIncluded,
                    SightseeingIncluded = existingDeal.SightseeingIncluded,
                    StayIncluded = existingDeal.StayIncluded,
                    AirTransfer = existingDeal.AirTransfer,
                    RoadTransfer = existingDeal.RoadTransfer,
                    TrainTransfer = existingDeal.TrainTransfer,
                    TravelCostIncluded = existingDeal.TravelCostIncluded,
                    GuideIncluded = existingDeal.GuideIncluded,
                    PhotographyIncluded = existingDeal.PhotographyIncluded,
                    InsuranceIncluded = existingDeal.InsuranceIncluded,
                    VisaIncluded = existingDeal.VisaIncluded,
                    UpdatedAt = DateTime.UtcNow,
                };

                var success = await _dbHelper.UpdateDeal(id, updateDto);
                if (!success)
                {
                    return StatusCode(500, "Failed to update deal");
                }

                // Return the updated deal
                var updatedDeal = await _dbHelper.GetDealById(id);
                return Ok(updatedDeal);
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

                // Update total deals count
                await _dbHelper.UpdateAgencyTotalDeals(userId, -1);

                return Ok(new { Success = true, Message = "Deal deleted successfully" });
            }
            catch (Exception ex)
            {
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
            [FromQuery] string? packageType,
            [FromQuery] string? difficultyLevel,
            [FromQuery] bool? isInstantBooking,
            [FromQuery] bool? isLastMinuteDeal
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
                    packageType,
                    difficultyLevel,
                    isInstantBooking,
                    isLastMinuteDeal,
                    null
                );

                return Ok(deals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while searching deals");
            }
        }

        // GET: api/Deal/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<DealResponseDto>>> GetDealsByUserId(
            string userId,
            [FromQuery] bool? isActive = null
        )
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID cannot be empty");
                }

                var deals = await _dbHelper.GetDealsByUserId(userId, null, isActive);

                return Ok(deals ?? new List<DealResponseDto>());
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                return StatusCode(500, "Database error occurred while retrieving deals");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving deals");
            }
        }

        // PUT: api/Deal/5/toggle-status
        [HttpPut("{id}/toggle-status")]
        public async Task<ActionResult<DealResponseDto>> ToggleDealStatus(
            int id,
            [FromBody] DealToggleStatusDto toggleDto
        )
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
                    return StatusCode(403, "You don't have permission to update this deal");
                }

                var success = await _dbHelper.ToggleDealStatus(id, toggleDto.IsActive);

                if (!success)
                {
                    return StatusCode(500, "Failed to update deal status");
                }

                // Get the updated deal and return it
                var updatedDeal = await _dbHelper.GetDealById(id);
                return Ok(updatedDeal);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the deal status");
            }
        }
    }
}
