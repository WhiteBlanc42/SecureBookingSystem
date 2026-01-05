using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureBookingSystem.Data;
using SecureBookingSystem.Models;

namespace SecureBookingSystem.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SecureBookingSystem.Services.IAuditService _auditService;

        public BookingsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SecureBookingSystem.Services.IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Admin"))
            {
                // Admin sees all bookings
                return View(await _context.Bookings.Include(b => b.User).Include(b => b.Room).ToListAsync());
            }
            else
            {
                // Normal user sees only their own bookings
                return View(await _context.Bookings.Include(b => b.Room).Where(b => b.UserId == user.Id).ToListAsync());
            }
        }

        // GET: Bookings/Create
        public async Task<IActionResult> Create()
        {
            var rooms = await _context.Rooms.Where(r => r.IsAvailable).ToListAsync();
            ViewBag.Rooms = rooms;
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,BookingDate,CheckOutDate,ServiceType")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                booking.UserId = user.Id;
                booking.Status = "Pending"; // Default status
                
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                
                _context.Add(booking);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(user.Id, "CreateBooking", $"Created booking for room '{room?.Name}' from {booking.BookingDate:d} to {booking.CheckOutDate:d}", HttpContext.Connection.RemoteIpAddress?.ToString());

                return RedirectToAction(nameof(Index));
            }
            var rooms = await _context.Rooms.Where(r => r.IsAvailable).ToListAsync();
            ViewBag.Rooms = rooms;
            return View(booking);
        }

        // GET: Bookings/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ServiceType,BookingDate,Status")] Booking booking)
        {
            if (id != booking.Id) return NotFound();

            var existingBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (existingBooking == null) return NotFound();

            // Preserve UserId
            booking.UserId = existingBooking.UserId;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    var user = await _userManager.GetUserAsync(User);
                    await _auditService.LogAsync(user.Id, "EditBooking", $"Updated booking {id}. Status: {booking.Status}", HttpContext.Connection.RemoteIpAddress?.ToString());
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && booking.UserId != user.Id)
            {
                return Forbid();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            
             // IDOR Check
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && booking.UserId != user.Id)
            {
                return Forbid();
            }
            
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
             await _auditService.LogAsync(user.Id, "DeleteBooking", $"Deleted booking {id}", HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}
