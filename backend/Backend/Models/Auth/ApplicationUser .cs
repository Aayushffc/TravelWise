using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Auth
{
    public class ApplicationUser : IdentityUser 
    {
        [MaxLength(200)]
        public required string FullName { get; set; }
    }
}
