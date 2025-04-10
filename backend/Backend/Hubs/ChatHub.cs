using System.Security.Claims;
using Backend.DTOs;
using Backend.Helper;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs
{
    [Authorize]
    public class ChatHub : Hub, IChatHub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IDBHelper _dbHelper;

        public ChatHub(ILogger<ChatHub> logger, IDBHelper dbHelper)
        {
            _logger = logger;
            _dbHelper = dbHelper;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                _logger.LogInformation($"User {userId} connected to chat hub");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                _logger.LogInformation($"User {userId} disconnected from chat hub");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinBookingChat(int bookingId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                throw new HubException("User not authenticated");
            }

            // Verify user has access to this booking
            var booking = await _dbHelper.GetBookingById(bookingId, userId);
            if (booking == null)
            {
                throw new HubException("Booking not found or access denied");
            }

            // Only allow chat for accepted bookings
            if (booking.Status != "Accepted")
            {
                throw new HubException("Chat is only available for accepted bookings");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
            _logger.LogInformation($"User {userId} joined booking chat {bookingId}");
        }

        public async Task LeaveBookingChat(int bookingId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation($"User {userId} left booking chat {bookingId}");
        }

        public async Task SendMessage(
            int bookingId,
            string message,
            string? messageType = null,
            string? fileUrl = null,
            string? fileName = null,
            long? fileSize = null
        )
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                throw new HubException("User not authenticated");
            }

            // Verify user has access to this booking
            var booking = await _dbHelper.GetBookingById(bookingId, userId);
            if (booking == null)
            {
                throw new HubException("Booking not found or access denied");
            }

            // Only allow chat for accepted bookings
            if (booking.Status != "Accepted")
            {
                throw new HubException("Chat is only available for accepted bookings");
            }

            // Determine receiver (if sender is customer, receiver is agency and vice versa)
            var receiverId = userId == booking.UserId ? booking.AgencyId : booking.UserId;

            // Save message to database
            var chatMessage = await _dbHelper.SendMessage(
                bookingId,
                new SendMessageDTO
                {
                    ReceiverId = receiverId,
                    Message = message,
                    MessageType = messageType,
                    FileUrl = fileUrl,
                    FileName = fileName,
                    FileSize = fileSize,
                },
                userId
            );

            if (chatMessage == null)
            {
                throw new HubException("Failed to send message");
            }

            // Notify the receiver
            await Clients
                .User(receiverId)
                .SendAsync(
                    "ReceiveMessage",
                    new
                    {
                        bookingId,
                        messageId = chatMessage.Id,
                        senderId = userId,
                        message,
                        sentAt = chatMessage.SentAt,
                        messageType,
                        fileUrl,
                        fileName,
                        fileSize,
                    }
                );

            // Update last message in booking
            await _dbHelper.UpdateBookingLastMessage(bookingId, message, userId);
        }
    }
}
