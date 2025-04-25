using Backend.DTOs;
using Backend.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FAQController : ControllerBase
    {
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<FAQController> _logger;

        public FAQController(IDBHelper dbHelper, ILogger<FAQController> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FAQResponseDTO>>> GetFAQs()
        {
            try
            {
                var faqs = await _dbHelper.GetFAQs();
                return Ok(faqs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving FAQs");
                return StatusCode(500, "An error occurred while retrieving FAQs");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FAQResponseDTO>>> SearchFAQs(
            [FromQuery] string query
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }

                var faqs = await _dbHelper.SearchFAQs(query);
                return Ok(faqs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching FAQs");
                return StatusCode(500, "An error occurred while searching FAQs");
            }
        }

        [HttpPost("question")]
        public async Task<ActionResult<FAQResponseDTO>> SubmitQuestion(
            [FromBody] FAQCreateDTO question
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(question.Question))
                {
                    return BadRequest("Question cannot be empty");
                }

                var faq = await _dbHelper.CreateFAQ(question);
                return CreatedAtAction(nameof(GetFAQs), new { id = faq.Id }, faq);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting question");
                return StatusCode(500, "An error occurred while submitting the question");
            }
        }

        [HttpPut("{id}/answer")]
        public async Task<IActionResult> AnswerQuestion(int id, [FromBody] FAQUpdateDTO answer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(answer.Answer))
                {
                    return BadRequest("Answer cannot be empty");
                }

                var faq = await _dbHelper.UpdateFAQ(id, answer);
                if (faq == null)
                {
                    return NotFound("Question not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error answering question");
                return StatusCode(500, "An error occurred while answering the question");
            }
        }
    }
}
