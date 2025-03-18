using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Backend.Models.Auth
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(200)]
        public required string FullName { get; set; }
    }
}
