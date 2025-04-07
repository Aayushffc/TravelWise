using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs
{
    [Authorize]
    public class ChatHub : Hub, IChatHub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
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
            await Groups.AddToGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
            _logger.LogInformation($"User joined booking chat {bookingId}");
        }

        public async Task LeaveBookingChat(int bookingId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
            _logger.LogInformation($"User left booking chat {bookingId}");
        }
    }
}
