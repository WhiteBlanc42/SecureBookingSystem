using System.ComponentModel.DataAnnotations;

namespace SecureBookingSystem.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Room Name")]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Range(1, 100000)]
        [Display(Name = "Price per Night")]
        [DataType(DataType.Currency)]
        public decimal PricePerNight { get; set; }

        [Required]
        [Range(1, 10)]
        [Display(Name = "Max Guests")]
        public int MaxGuests { get; set; }

        [StringLength(50)]
        [Display(Name = "Room Type")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Room type can only contain letters and spaces")]
        public string RoomType { get; set; } // e.g., Single, Double, Suite, Deluxe

        [Display(Name = "Image")]
        public string ImagePath { get; set; } // Stored as relative path or filename

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; } = true;
    }
}
