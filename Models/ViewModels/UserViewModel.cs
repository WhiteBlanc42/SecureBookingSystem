using Microsoft.AspNetCore.Identity;

namespace SecureBookingSystem.Models.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Roles { get; set; }
        public bool IsAdmin { get; set; }
    }
}
