using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SecureBookingSystem.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public virtual IdentityUser User { get; set; }

        [Required]
        [Display(Name = "Room")]
        public int RoomId { get; set; }
        public virtual Room Room { get; set; }

        [Display(Name = "Service Type")]
        [StringLength(100)]
        public string ServiceType { get; set; } // Optional notes

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-in Date")]
        public DateTime BookingDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-out Date")]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression(@"^(Pending|Confirmed|Cancelled)$", ErrorMessage = "Invalid Status")]
        public string Status { get; set; } = "Pending";
    }
}

