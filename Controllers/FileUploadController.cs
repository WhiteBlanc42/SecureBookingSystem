using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SecureBookingSystem.Controllers
{
    [Authorize]
    public class FileUploadController : Controller
    {
        // Allowed extensions and MIME types
        private readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
        private readonly string[] _permittedMimeTypes = { "image/jpeg", "image/png", "application/pdf" };
        private readonly long _fileSizeLimit = 2 * 1024 * 1024; // 2 MB

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null)
            {
                ModelState.AddModelError("", "No file uploaded.");
                return View("Index");
            }

            // 1. Validate File Size
            if (file.Length > _fileSizeLimit)
            {
                ModelState.AddModelError("", "File size exceeds the 2MB limit.");
                return View("Index");
            }

            // 2. Validate Extension
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !_permittedExtensions.Contains(ext))
            {
                ModelState.AddModelError("", "Invalid file extension.");
                return View("Index");
            }

            // 3. Validate MIME Type
            if (!_permittedMimeTypes.Contains(file.ContentType))
            {
                ModelState.AddModelError("", "Invalid content type.");
                return View("Index");
            }

            // 4. Secure Storage (Rename to UUID)
            var trustedFileName = Guid.NewGuid().ToString() + ext;
            
            // Store outside web root (simulated by using a safe directory)
            // In a real scenario, this would be strictly outside the wwwroot or in Blob Storage.
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }
            
            var filePath = Path.Combine(uploadPath, trustedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            ViewBag.Message = "File uploaded successfully!";
            return View("Index");
        }
    }
}
