using Backend.DTOs;
using Backend.Helper;
using Backend.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TravelWiseAPI.Controllers
{
    [Route("api/agency-profiles")]
    [ApiController]
    [Authorize]
    public class AgencyProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<AgencyProfileController> _logger;

        public AgencyProfileController(
            UserManager<ApplicationUser> userManager,
            IDBHelper dbHelper,
            ILogger<AgencyProfileController> logger
        )
        {
            _userManager = userManager;
            _dbHelper = dbHelper;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAgencyProfile(
            [FromBody] CreateAgencyProfileDTO model
        )
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(
                        new
                        {
                            message = "Invalid profile data",
                            errors = ModelState.Values.SelectMany(v => v.Errors),
                        }
                    );

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var profile = await _dbHelper.CreateAgencyProfile(model, user.Id);
                if (profile == null)
                    return StatusCode(500, new { message = "Failed to create agency profile" });

                return Ok(
                    new { message = "Agency profile created successfully", profileId = profile.Id }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating agency profile");
                return StatusCode(
                    500,
                    new { message = "An error occurred while creating the agency profile" }
                );
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAgencyProfile(
            int id,
            [FromBody] UpdateAgencyProfileDTO model
        )
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(
                        new
                        {
                            message = "Invalid profile data",
                            errors = ModelState.Values.SelectMany(v => v.Errors),
                        }
                    );

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.UpdateAgencyProfile(id, model, user.Id);
                if (!result)
                    return NotFound(
                        new { message = "Agency profile not found or you don't have permission" }
                    );

                return Ok(new { message = "Agency profile updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agency profile");
                return StatusCode(
                    500,
                    new { message = "An error occurred while updating the agency profile" }
                );
            }
        }

        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyAgencyProfile()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var profile = await _dbHelper.GetAgencyProfileByUserId(user.Id);
                if (profile == null)
                    return NotFound(new { message = "Agency profile not found" });

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agency profile");
                return StatusCode(
                    500,
                    new { message = "An error occurred while getting the agency profile" }
                );
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAgencyProfile(int id)
        {
            try
            {
                var profile = await _dbHelper.GetAgencyProfileById(id);
                if (profile == null)
                    return NotFound(new { message = "Agency profile not found" });

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agency profile");
                return StatusCode(
                    500,
                    new { message = "An error occurred while getting the agency profile" }
                );
            }
        }

        [HttpPost("online-status")]
        public async Task<IActionResult> UpdateOnlineStatus([FromBody] bool isOnline)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.UpdateAgencyOnlineStatus(user.Id, isOnline);
                if (!result)
                    return NotFound(new { message = "Agency profile not found" });

                return Ok(new { message = "Online status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating online status");
                return StatusCode(
                    500,
                    new { message = "An error occurred while updating online status" }
                );
            }
        }

        [HttpPost("last-active")]
        public async Task<IActionResult> UpdateLastActive()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.UpdateAgencyLastActive(user.Id);
                if (!result)
                    return NotFound(new { message = "Agency profile not found" });

                return Ok(new { message = "Last active time updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last active time");
                return StatusCode(
                    500,
                    new { message = "An error occurred while updating last active time" }
                );
            }
        }
    }
}
