using Backend.DTOs;
using Backend.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportTicketController : ControllerBase
    {
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<SupportTicketController> _logger;

        public SupportTicketController(IDBHelper dbHelper, ILogger<SupportTicketController> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<SupportTicketResponseDTO>> CreateTicket(
            [FromBody] SupportTicketCreateDTO ticket
        )
        {
            try
            {
                if (
                    string.IsNullOrWhiteSpace(ticket.Name)
                    || string.IsNullOrWhiteSpace(ticket.Email)
                    || string.IsNullOrWhiteSpace(ticket.ProblemTitle)
                    || string.IsNullOrWhiteSpace(ticket.ProblemDescription)
                )
                {
                    return BadRequest("All fields are required");
                }

                var createdTicket = await _dbHelper.CreateSupportTicket(ticket);
                return CreatedAtAction(
                    nameof(GetTicket),
                    new { id = createdTicket.Id },
                    createdTicket
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating support ticket");
                return StatusCode(500, "An error occurred while creating the support ticket");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupportTicketResponseDTO>> GetTicket(int id)
        {
            try
            {
                var ticket = await _dbHelper.GetSupportTicketById(id);
                if (ticket == null)
                {
                    return NotFound("Ticket not found");
                }

                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving support ticket");
                return StatusCode(500, "An error occurred while retrieving the support ticket");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupportTicketResponseDTO>>> GetTickets(
            [FromQuery] string? status = null
        )
        {
            try
            {
                var tickets = await _dbHelper.GetSupportTickets(status);
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving support tickets");
                return StatusCode(500, "An error occurred while retrieving support tickets");
            }
        }

        [HttpPut("{id}/respond")]
        public async Task<IActionResult> RespondToTicket(
            int id,
            [FromBody] SupportTicketUpdateDTO response
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response.AdminResponse))
                {
                    return BadRequest("Response cannot be empty");
                }

                var ticket = await _dbHelper.UpdateSupportTicket(id, response);
                if (ticket == null)
                {
                    return NotFound("Ticket not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to support ticket");
                return StatusCode(500, "An error occurred while responding to the support ticket");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTicketStatus(int id, [FromBody] string status)
        {
            try
            {
                if (!new[] { "Open", "In Progress", "Resolved", "Closed" }.Contains(status))
                {
                    return BadRequest("Invalid status");
                }

                var success = await _dbHelper.UpdateSupportTicketStatus(id, status);
                if (!success)
                {
                    return NotFound("Ticket not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket status");
                return StatusCode(500, "An error occurred while updating the ticket status");
            }
        }
    }
}
