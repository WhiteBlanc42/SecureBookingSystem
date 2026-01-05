using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SecureBookingSystem.Services;

namespace SecureBookingSystem.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService;

        public LogoutModel(SignInManager<IdentityUser> signInManager, 
            ILogger<LogoutModel> logger,
            UserManager<IdentityUser> userManager,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                 // Audit Log before sign out while we still have the user info (or at least the ID)
                 await _auditService.LogAsync(user.Id, "Logout", "User logged out", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage();
            }
        }
    }
}
