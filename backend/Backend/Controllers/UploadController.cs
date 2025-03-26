using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IS3Service _s3Service;

        public UploadController(IS3Service s3Service)
        {
            _s3Service = s3Service;
        }

        [HttpPost("file")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string folder)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                    return BadRequest("Invalid file type. Only JPEG, PNG, and GIF files are allowed.");

                // Validate file size (10MB max)
                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest("File size exceeds 10MB limit");

                var result = await _s3Service.UploadFileAsync(file, folder);
                return Ok(new { url = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("file/{key}")]
        public async Task<IActionResult> DeleteFile(string key)
        {
            try
            {
                var success = await _s3Service.DeleteFileAsync(key);
                if (success)
                    return Ok();
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}