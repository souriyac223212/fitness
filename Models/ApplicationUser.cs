using Microsoft.AspNetCore.Identity;

namespace FitWiseBD.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}