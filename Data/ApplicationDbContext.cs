using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SecureBookingSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SecureBookingSystem.Models.Booking> Bookings { get; set; }
        public DbSet<SecureBookingSystem.Models.AuditLog> AuditLogs { get; set; }
        public DbSet<SecureBookingSystem.Models.Room> Rooms { get; set; }
    }
}
