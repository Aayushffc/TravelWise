using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs
{
    public interface IChatHub
    {
        Task JoinBookingChat(int bookingId);
        Task LeaveBookingChat(int bookingId);
    }
}
