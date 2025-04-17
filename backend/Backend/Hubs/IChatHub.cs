using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs
{
    public interface IChatHub
    {
        Task JoinBookingChat(int bookingId);
        Task LeaveBookingChat(int bookingId);
        Task SendMessage(
            int bookingId,
            string message,
            string? messageType = null,
            string? fileUrl = null,
            string? fileName = null,
            long? fileSize = null
        );
    }
}
