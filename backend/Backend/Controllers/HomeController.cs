using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Backend.DTOs;
using Backend.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IDBHelper dbHelper, ILogger<HomeController> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<ActionResult<SearchResponseDto>> SearchDeals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int? minDays = null,
            [FromQuery] int? maxDays = null,
            [FromQuery] string? packageType = null,
            [FromQuery] string? location = null,
            [FromQuery] string? country = null,
            [FromQuery] string? continent = null,
            [FromQuery] string? agencyId = null,
            [FromQuery] string? tags = null,
            [FromQuery] string? categories = null,
            [FromQuery] string? seasons = null,
            [FromQuery] string? difficultyLevel = null,
            [FromQuery] bool? isInstantBooking = null,
            [FromQuery] bool? isLastMinuteDeal = null,
            [FromQuery] DateTime? validFrom = null,
            [FromQuery] DateTime? validUntil = null,
            [FromQuery] string? sortBy = "relevance" // relevance, price_asc, price_desc, rating, discount, newest
        )
        {
            try
            {
                var deals = await _dbHelper.GetDeals(null);

                // Apply search filters - make status filtering more lenient
                var filteredDeals = deals;

                // Apply search term
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    _logger.LogInformation($"Searching for term: '{searchTerm}'");

                    var beforeCount = filteredDeals.Count();
                    filteredDeals = filteredDeals.Where(d =>
                        (d.Title != null && d.Title.ToLower().Contains(searchTerm))
                        || (d.Description != null && d.Description.ToLower().Contains(searchTerm))
                        || (
                            d.Location?.Name != null
                            && d.Location.Name.ToLower().Contains(searchTerm)
                        )
                        || (
                            d.Location?.Country != null
                            && d.Location.Country.ToLower().Contains(searchTerm)
                        )
                        || (
                            d.Location?.Continent != null
                            && d.Location.Continent.ToLower().Contains(searchTerm)
                        )
                        || (
                            d.SearchKeywords != null
                            && d.SearchKeywords.ToLower().Contains(searchTerm)
                        )
                        // Check exact match on title as well
                        || (d.Title != null && d.Title.ToLower() == searchTerm)
                    );
                }

                // Apply price filter
                if (minPrice.HasValue)
                    filteredDeals = filteredDeals.Where(d => d.Price >= minPrice.Value);
                if (maxPrice.HasValue)
                    filteredDeals = filteredDeals.Where(d => d.Price <= maxPrice.Value);

                // Apply days filter
                if (minDays.HasValue)
                    filteredDeals = filteredDeals.Where(d => d.DaysCount >= minDays.Value);
                if (maxDays.HasValue)
                    filteredDeals = filteredDeals.Where(d => d.DaysCount <= maxDays.Value);

                // Apply location filters
                if (!string.IsNullOrEmpty(location))
                    filteredDeals = filteredDeals.Where(d =>
                        d.Location?.Name?.ToLower().Contains(location.ToLower()) == true
                    );
                if (!string.IsNullOrEmpty(country))
                    filteredDeals = filteredDeals.Where(d =>
                        d.Location?.Country?.ToLower() == country.ToLower()
                    );
                if (!string.IsNullOrEmpty(continent))
                    filteredDeals = filteredDeals.Where(d =>
                        d.Location?.Continent?.ToLower() == continent.ToLower()
                    );

                // Apply package type filter
                if (!string.IsNullOrEmpty(packageType))
                    filteredDeals = filteredDeals.Where(d => d.PackageType == packageType);

                // Apply additional filters
                if (!string.IsNullOrEmpty(tags))
                    filteredDeals = filteredDeals.Where(d => d.Tags?.Contains(tags) == true);
                if (!string.IsNullOrEmpty(seasons))
                    filteredDeals = filteredDeals.Where(d => d.Seasons?.Contains(seasons) == true);
                if (!string.IsNullOrEmpty(difficultyLevel))
                    filteredDeals = filteredDeals.Where(d => d.DifficultyLevel == difficultyLevel);
                if (isInstantBooking.HasValue)
                    filteredDeals = filteredDeals.Where(d =>
                        d.IsInstantBooking == isInstantBooking.Value
                    );
                if (isLastMinuteDeal.HasValue)
                    filteredDeals = filteredDeals.Where(d =>
                        d.IsLastMinuteDeal == isLastMinuteDeal.Value
                    );
                if (validFrom.HasValue)
                    filteredDeals = filteredDeals.Where(d => d.ValidFrom >= validFrom.Value);
                if (validUntil.HasValue)
                    filteredDeals = filteredDeals.Where(d => d.ValidUntil <= validUntil.Value);

                // Apply sorting
                filteredDeals = sortBy switch
                {
                    "price_asc" => filteredDeals.OrderBy(d => d.Price),
                    "price_desc" => filteredDeals.OrderByDescending(d => d.Price),
                    "rating" => filteredDeals.OrderByDescending(d => d.Rating),
                    "discount" => filteredDeals.OrderByDescending(d => d.DiscountPercentage),
                    "newest" => filteredDeals.OrderByDescending(d => d.CreatedAt),
                    "popular" => filteredDeals.OrderByDescending(d => d.ClickCount),
                    _ => filteredDeals.OrderByDescending(d => d.RelevanceScore),
                };

                // Apply pagination
                var totalCount = filteredDeals.Count();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedDeals = filteredDeals
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // We're no longer updating view counts automatically for search results to avoid SQL parameter issues

                return Ok(
                    new SearchResponseDto
                    {
                        Deals = paginatedDeals,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        CurrentPage = page,
                        PageSize = pageSize,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while searching deals");
            }
        }

        [HttpPost("deal/{id}/click")]
        public async Task<IActionResult> RecordDealClick(int id)
        {
            try
            {
                var deal = await _dbHelper.GetDealById(id);
                if (deal == null)
                    return NotFound($"Deal with ID {id} not found");

                deal.ClickCount++;
                deal.LastClicked = DateTime.UtcNow;
                deal.RelevanceScore = CalculateRelevanceScore(deal);

                await _dbHelper.UpdateDeal(
                    id,
                    new DealUpdateDto
                    {
                        ClickCount = deal.ClickCount,
                        LastClicked = deal.LastClicked,
                        RelevanceScore = deal.RelevanceScore,
                        UpdatedAt = DateTime.UtcNow,
                    }
                );

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while recording the click");
            }
        }

        private decimal CalculateRelevanceScore(DealResponseDto deal)
        {
            var score = 0m;

            // Base score from clicks and views
            score += deal.ClickCount * 0.3m;
            score += deal.ViewCount * 0.1m;

            // Rating impact
            score += deal.Rating * 2;

            // Discount impact
            score += deal.DiscountPercentage * 0.5m;

            // Recency impact
            var daysSinceCreation = (decimal)(DateTime.UtcNow - deal.CreatedAt).TotalDays;
            score += Math.Max(0, 10 - (daysSinceCreation / 30));

            return score;
        }
    }
}
