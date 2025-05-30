using Backend.DTOs;

namespace Backend.Services
{
    public interface IStripeConnectService
    {
        Task<StripeConnectStatusDTO> GetConnectStatus(string userId);
        Task<StripeConnectAccountDTO> CreateConnectAccount(string userId);
        Task<StripeAccountLinkDTO> CreateAccountLink(string userId);
        Task UpdateConnectAccountStatus(string stripeAccountId);
    }
}
