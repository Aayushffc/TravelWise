using Backend.Models.Auth;

namespace Backend.Helper
{
    public interface IDBHelper
    {
        Task<string> GenerateJwtToken(ApplicationUser user);
        Task<bool> AddUserRole(ApplicationUser user);
        Task<bool> AddAgencyRole(ApplicationUser user);
        Task<bool> AddAdminRole(ApplicationUser user);
    }
}
