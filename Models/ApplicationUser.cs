using Microsoft.AspNetCore.Identity;
namespace OnlineMovieTicket.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        //Role Assigment
        public UserRole Role { get; set; } // Enum to define user roles (Admin, Manager, User)
        public DateTime CreatedAt { get; set; } // Timestamp for when the user was created
        public DateTime UpdatedAt { get; set; } // Timestamp for when the user was last updated
        public string CreatedBy { get; set; } // User who created this user
        public string UpdatedBy { get; set; } // User who last updated this user
        //Navigation properties
        public ICollection<Booking> Bookings { get; set; } // List of bookings made by the user
    }
}
