using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecureBookingSystem.Models;

namespace SecureBookingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            var model = new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = statusCode
            };

            if (statusCode.HasValue)
            {
                switch (statusCode.Value)
                {
                    case 404:
                        model.Message = "The resource you requested could not be found.";
                        break;
                    case 403:
                        model.Message = "You do not have permission to access this resource.";
                        break;
                    case 500:
                        model.Message = "An internal server error occurred.";
                        break;
                    default:
                        model.Message = "An unexpected error occurred.";
                        break;
                }
            }

            return View(model);
        }
    }
}
