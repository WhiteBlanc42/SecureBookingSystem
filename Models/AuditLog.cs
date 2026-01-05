using System;
using System.ComponentModel.DataAnnotations;

namespace SecureBookingSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Action { get; set; } // Login, CreateBooking, etc.
        
        [StringLength(1000)]
        public string Details { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [StringLength(45)]
        public string IpAddress { get; set; }
    }
}
