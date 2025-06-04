using Microsoft.AspNetCore.Identity;
namespace OnlineMovieTicket.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        //Navigation properties
        public ICollection<Booking> Bookings { get; set; } // List of bookings made by the user
    }
}
