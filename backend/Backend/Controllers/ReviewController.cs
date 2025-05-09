using System.Text.Json;
using Backend.DTOs;
using Backend.Helper;
using Backend.Models.Auth;
using Backend.Models.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/reviews")]
    [ApiController]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IDBHelper _dbHelper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            IDBHelper dbHelper,
            UserManager<ApplicationUser> userManager,
            ILogger<ReviewController> logger
        )
        {
            _dbHelper = dbHelper;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(
                        new
                        {
                            message = "Invalid review data",
                            errors = ModelState.Values.SelectMany(v => v.Errors),
                        }
                    );

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.CreateReview(model, user.Id);
                if (!result)
                    return BadRequest(
                        new
                        {
                            message = "Failed to create review. Please check if you have already reviewed this deal.",
                        }
                    );

                return Ok(new { message = "Review created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(
                        new
                        {
                            message = "Invalid review data",
                            errors = ModelState.Values.SelectMany(v => v.Errors),
                        }
                    );

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.UpdateReview(id, model, user.Id);
                if (!result)
                    return BadRequest(
                        new
                        {
                            message = "Failed to update review. The review may not exist or you may not have permission to update it.",
                        }
                    );

                return Ok(new { message = "Review updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.DeleteReview(id, user.Id);
                if (!result)
                    return BadRequest(new { message = "Failed to delete review" });

                return Ok(new { message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review");
                return StatusCode(
                    500,
                    new { message = "An error occurred while deleting the review" }
                );
            }
        }

        [HttpGet("deal/{dealId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDealReviews(int dealId)
        {
            try
            {
                var reviews = await _dbHelper.GetDealReviews(dealId);
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deal reviews");
                return StatusCode(
                    500,
                    new { message = "An error occurred while getting the reviews" }
                );
            }
        }
    }
}
