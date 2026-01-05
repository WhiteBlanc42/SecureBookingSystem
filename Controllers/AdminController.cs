using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SecureBookingSystem.Data;
using SecureBookingSystem.Models;
using SecureBookingSystem.Models.ViewModels;
using SecureBookingSystem.Services;

namespace SecureBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService;

        // Secure file upload settings
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.PendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending");
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalRooms = await _context.Rooms.CountAsync();
            
            return View();
        }

        #region User Management

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<UserViewModel>();
            
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = string.Join(", ", roles),
                    IsAdmin = roles.Contains("Admin")
                });
            }
            
            return View(userList);
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent admin from deleting themselves
            var currentAdmin = await _userManager.GetUserAsync(User);
            if (user.Id == currentAdmin.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            // Delete user's bookings first
            var userBookings = _context.Bookings.Where(b => b.UserId == id);
            _context.Bookings.RemoveRange(userBookings);
            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _auditService.LogAsync(currentAdmin.Id, "AdminDeleteUser", $"Admin deleted user: {user.Email}", HttpContext.Connection.RemoteIpAddress?.ToString());
                TempData["Success"] = $"User {user.Email} has been deleted.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(Users));
        }

        #endregion

        #region Booking Management

        // GET: Admin/Bookings
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Bookings.Include(b => b.User).Include(b => b.Room).OrderByDescending(b => b.BookingDate).ToListAsync();
            return View(bookings);
        }

        // GET: Admin/CreateBooking
        public async Task<IActionResult> CreateBooking()
        {
            var users = await _userManager.Users.ToListAsync();
            ViewBag.Users = new SelectList(users, "Id", "Email");
            return View();
        }

        // POST: Admin/CreateBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking([Bind("UserId,ServiceType,BookingDate,Status")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();

                var admin = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(admin.Id, "AdminCreateBooking", $"Admin created booking for user {booking.UserId}: {booking.ServiceType}", HttpContext.Connection.RemoteIpAddress?.ToString());

                return RedirectToAction(nameof(Bookings));
            }
            var users = await _userManager.Users.ToListAsync();
            ViewBag.Users = new SelectList(users, "Id", "Email");
            return View(booking);
        }

        // GET: Admin/EditBooking/5
        public async Task<IActionResult> EditBooking(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.Include(b => b.User).Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            ViewBag.UserEmail = booking.User?.Email ?? "Unknown";
            ViewBag.RoomName = booking.Room?.Name ?? "Unknown";
            return View(booking);
        }

        // POST: Admin/EditBooking/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBooking(int id, [Bind("Id,UserId,RoomId,ServiceType,BookingDate,CheckOutDate,Status")] Booking booking)
        {
            if (id != booking.Id) return NotFound();

            // Preserve original UserId and RoomId (cannot be changed by admin)
            var existingBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (existingBooking == null) return NotFound();
            
            booking.UserId = existingBooking.UserId;
            booking.RoomId = existingBooking.RoomId;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();

                    var admin = await _userManager.GetUserAsync(User);
                    await _auditService.LogAsync(admin.Id, "AdminEditBooking", $"Admin updated booking {id}. Status: {booking.Status}", HttpContext.Connection.RemoteIpAddress?.ToString());
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Bookings.Any(e => e.Id == booking.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Bookings));
            }
            
            var bookingWithDetails = await _context.Bookings.Include(b => b.User).Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
            ViewBag.UserEmail = bookingWithDetails?.User?.Email ?? "Unknown";
            ViewBag.RoomName = bookingWithDetails?.Room?.Name ?? "Unknown";
            return View(booking);
        }

        // POST: Admin/DeleteBooking/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            var admin = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(admin.Id, "AdminDeleteBooking", $"Admin deleted booking {id}", HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToAction(nameof(Bookings));
        }

        // POST: Admin/UpdateStatus (AJAX-friendly)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = status;
            _context.Update(booking);
            await _context.SaveChangesAsync();

            var admin = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(admin.Id, "AdminUpdateStatus", $"Admin changed booking {id} status to {status}", HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToAction(nameof(Bookings));
        }

        #endregion

        #region Room Catalog Management

        // GET: Admin/Catalog
        public async Task<IActionResult> Catalog()
        {
            var rooms = await _context.Rooms.OrderBy(r => r.Name).ToListAsync();
            return View(rooms);
        }

        // GET: Admin/CreateRoom
        public IActionResult CreateRoom()
        {
            return View();
        }

        // POST: Admin/CreateRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoom(Room room, IFormFile imageFile)
        {
            if (imageFile != null)
            {
                var uploadResult = await SaveImageSecurely(imageFile);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("ImagePath", uploadResult.ErrorMessage);
                    return View(room);
                }
                room.ImagePath = uploadResult.FileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();

                var admin = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(admin.Id, "AdminCreateRoom", $"Admin created room: {room.Name}", HttpContext.Connection.RemoteIpAddress?.ToString());

                return RedirectToAction(nameof(Catalog));
            }
            return View(room);
        }

        // GET: Admin/EditRoom/5
        public async Task<IActionResult> EditRoom(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Admin/EditRoom/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoom(int id, Room room, IFormFile imageFile)
        {
            if (id != room.Id) return NotFound();

            var existingRoom = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (existingRoom == null) return NotFound();

            // Handle new image upload
            if (imageFile != null)
            {
                var uploadResult = await SaveImageSecurely(imageFile);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("ImagePath", uploadResult.ErrorMessage);
                    return View(room);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingRoom.ImagePath))
                {
                    DeleteImage(existingRoom.ImagePath);
                }

                room.ImagePath = uploadResult.FileName;
            }
            else
            {
                // Keep existing image
                room.ImagePath = existingRoom.ImagePath;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();

                    var admin = await _userManager.GetUserAsync(User);
                    await _auditService.LogAsync(admin.Id, "AdminEditRoom", $"Admin updated room {id}: {room.Name}", HttpContext.Connection.RemoteIpAddress?.ToString());
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Rooms.Any(e => e.Id == room.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Catalog));
            }
            return View(room);
        }

        // POST: Admin/DeleteRoom/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            // Delete associated image
            if (!string.IsNullOrEmpty(room.ImagePath))
            {
                DeleteImage(room.ImagePath);
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            var admin = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(admin.Id, "AdminDeleteRoom", $"Admin deleted room {id}: {room.Name}", HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToAction(nameof(Catalog));
        }

        // GET: Admin/GetRoomImage/{fileName}
        [AllowAnonymous]
        public IActionResult GetRoomImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return NotFound();

            // Sanitize filename to prevent path traversal
            fileName = Path.GetFileName(fileName); 
            
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Rooms");
            var filePath = Path.Combine(uploadsFolder, fileName);
            
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            string contentType;
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    contentType = "image/jpeg";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                case ".gif":
                    contentType = "image/gif";
                    break;
                case ".webp":
                    contentType = "image/webp";
                    break;
                default:
                    contentType = "application/octet-stream";
                    break;
            }

            return PhysicalFile(filePath, contentType);
        }

        #endregion

        #region Secure File Upload Helpers

        private async Task<(bool Success, string FileName, string ErrorMessage)> SaveImageSecurely(IFormFile file)
        {
            // Validate file size
            if (file.Length > MaxFileSize)
            {
                return (false, null, $"File size exceeds the maximum allowed size of {MaxFileSize / (1024 * 1024)}MB.");
            }

            // Validate extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return (false, null, $"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Validate MIME type
            if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return (false, null, $"Invalid file content type.");
            }

            // Generate secure filename with UUID
            var secureFileName = $"{Guid.NewGuid()}{extension}";

            // Store outside web root for security
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Rooms");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, secureFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, secureFileName, null);
        }

        private void DeleteImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            // Sanitize filename to prevent path traversal
            fileName = Path.GetFileName(fileName);
            
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Rooms");
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        #endregion
    }
}

