using Backend.DBContext;
using Backend.DTOs;
using Backend.Helper;
using Backend.Models;
using Backend.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace TravelWiseAPI.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<BookingController> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public BookingController(
            UserManager<ApplicationUser> userManager,
            IDBHelper dbHelper,
            ILogger<BookingController> logger,
            IHubContext<ChatHub> hubContext
        )
        {
            _userManager = userManager;
            _dbHelper = dbHelper;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(
                        new
                        {
                            message = "Invalid booking data",
                            errors = ModelState.Values.SelectMany(v => v.Errors),
                        }
                    );

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var booking = await _dbHelper.CreateBooking(model, user.Id);
                if (booking == null)
                    return StatusCode(500, new { message = "Failed to create booking" });

                // Notify agency about new booking
                await _hubContext
                    .Clients.User(booking.AgencyId)
                    .SendAsync(
                        "NewBooking",
                        new
                        {
                            bookingId = booking.Id,
                            userId = user.Id,
                            userName = user.FullName,
                            dealId = booking.DealId,
                            createdAt = booking.CreatedAt,
                        }
                    );

                // Increment booking count for the deal
                await _dbHelper.IncrementBookingCount(booking.DealId);

                return Ok(new { message = "Booking created successfully", bookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                return StatusCode(
                    500,
                    new { message = "An error occurred while creating the booking" }
                );
            }
        }

        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var bookings = await _dbHelper.GetBookings(user.Id);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings");
                return StatusCode(
                    500,
                    new { message = "An error occurred while getting bookings" }
                );
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var booking = await _dbHelper.GetBookingById(id, user.Id);
                if (booking == null)
                    return NotFound(new { message = "Booking not found" });

                return Ok(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking");
                return StatusCode(
                    500,
                    new { message = "An error occurred while getting the booking" }
                );
            }
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptBooking(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.UpdateBookingStatus(id, "Accepted", null, user.Id);
                if (!result)
                    return NotFound(
                        new { message = "Booking not found or you don't have permission" }
                    );

                var booking = await _dbHelper.GetBookingById(id, user.Id);
                if (booking != null)
                {
                    await _hubContext
                        .Clients.User(booking.UserId)
                        .SendAsync(
                            "BookingAccepted",
                            new
                            {
                                bookingId = booking.Id,
                                agencyId = user.Id,
                                agencyName = user.FullName,
                            }
                        );
                }

                return Ok(new { message = "Booking accepted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting booking");
                return StatusCode(
                    500,
                    new { message = "An error occurred while accepting the booking" }
                );
            }
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectBooking(int id, [FromBody] RejectBookingDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Invalid rejection data" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.UpdateBookingStatus(
                    id,
                    "Rejected",
                    model.Reason,
                    user.Id
                );
                if (!result)
                    return NotFound(
                        new { message = "Booking not found or you don't have permission" }
                    );

                var booking = await _dbHelper.GetBookingById(id, user.Id);
                if (booking != null)
                {
                    await _hubContext
                        .Clients.User(booking.UserId)
                        .SendAsync(
                            "BookingRejected",
                            new
                            {
                                bookingId = booking.Id,
                                agencyId = user.Id,
                                agencyName = user.FullName,
                                reason = model.Reason,
                            }
                        );
                }

                return Ok(new { message = "Booking rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting booking");
                return StatusCode(
                    500,
                    new { message = "An error occurred while rejecting the booking" }
                );
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Invalid cancellation data" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.UpdateBookingStatus(
                    id,
                    "Cancelled",
                    model.Reason,
                    user.Id
                );
                if (!result)
                    return NotFound(
                        new { message = "Booking not found or you don't have permission" }
                    );

                var booking = await _dbHelper.GetBookingById(id, user.Id);
                if (booking != null)
                {
                    var notifyUserId =
                        user.Id == booking.UserId ? booking.AgencyId : booking.UserId;
                    await _hubContext
                        .Clients.User(notifyUserId)
                        .SendAsync(
                            "BookingCancelled",
                            new
                            {
                                bookingId = booking.Id,
                                cancelledBy = user.Id,
                                cancelledByName = user.FullName,
                                reason = model.Reason,
                            }
                        );
                }

                return Ok(new { message = "Booking cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking");
                return StatusCode(
                    500,
                    new { message = "An error occurred while cancelling the booking" }
                );
            }
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteBooking(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.UpdateBookingStatus(id, "Completed", null, user.Id);
                if (!result)
                    return NotFound(
                        new { message = "Booking not found or you don't have permission" }
                    );

                var booking = await _dbHelper.GetBookingById(id, user.Id);
                if (booking != null)
                {
                    await _hubContext
                        .Clients.User(booking.UserId)
                        .SendAsync(
                            "BookingCompleted",
                            new
                            {
                                bookingId = booking.Id,
                                agencyId = user.Id,
                                agencyName = user.FullName,
                            }
                        );
                }

                return Ok(new { message = "Booking completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing booking");
                return StatusCode(
                    500,
                    new { message = "An error occurred while completing the booking" }
                );
            }
        }

        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetChatMessages(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var messages = await _dbHelper.GetChatMessages(id, user.Id);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat messages");
                return StatusCode(
                    500,
                    new { message = "An error occurred while getting chat messages" }
                );
            }
        }

        [HttpPost("{id}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Invalid message data" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var message = await _dbHelper.SendMessage(id, model, user.Id);
                if (message == null)
                    return StatusCode(500, new { message = "Failed to send message" });

                await _hubContext
                    .Clients.User(message.ReceiverId)
                    .SendAsync(
                        "NewMessage",
                        new
                        {
                            bookingId = message.BookingId,
                            messageId = message.Id,
                            senderId = user.Id,
                            senderName = user.FullName,
                            message = message.Message,
                            sentAt = message.SentAt,
                            messageType = message.MessageType,
                            fileUrl = message.FileUrl,
                            fileName = message.FileName,
                            fileSize = message.FileSize,
                        }
                    );

                return Ok(new { message = "Message sent successfully", messageId = message.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(
                    500,
                    new { message = "An error occurred while sending the message" }
                );
            }
        }

        [HttpPost("messages/{messageId}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int messageId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var result = await _dbHelper.MarkMessageAsRead(messageId, user.Id);
                if (!result)
                    return NotFound(
                        new { message = "Message not found or you don't have permission" }
                    );

                return Ok(new { message = "Message marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
                return StatusCode(
                    500,
                    new { message = "An error occurred while marking the message as read" }
                );
            }
        }
    }
}
