using Backend.DTOs;
using Backend.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly IDBHelper _dbHelper;

        public LocationController(IDBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // GET: api/Location
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetLocations()
        {
            var locations = await _dbHelper.GetLocations();
            return Ok(locations);
        }

        // GET: api/Location/popular
        [HttpGet("popular")]
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetPopularLocations()
        {
            var locations = await _dbHelper.GetPopularLocations();
            return Ok(locations);
        }

        // GET: api/Location/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LocationResponseDto>> GetLocation(int id)
        {
            var location = await _dbHelper.GetLocationById(id);

            if (location == null)
            {
                return NotFound();
            }

            await _dbHelper.IncrementLocationClickCount(id);
            return location;
        }

        // POST: api/Location
        [HttpPost]
        public async Task<ActionResult<LocationResponseDto>> CreateLocation(
            LocationCreateDto locationDto
        )
        {
            var location = await _dbHelper.CreateLocation(locationDto);
            return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, location);
        }

        // PUT: api/Location/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, LocationUpdateDto locationDto)
        {
            var success = await _dbHelper.UpdateLocation(id, locationDto);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Location/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var success = await _dbHelper.DeleteLocation(id);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST: api/Location/5/request-call
        [HttpPost("{id}/request-call")]
        public async Task<IActionResult> IncrementRequestCallCount(int id)
        {
            var success = await _dbHelper.IncrementLocationRequestCallCount(id);

            if (!success)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
