using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Models.ViewModels
{
    public class UserManagementViewModel
    {
        public List<ApplicationUser> Users { get; set; } = new();
        public int TotalUsers { get; set; }
        public int AdminCount { get; set; }
        public int ManagerCount { get; set; }
        public int CustomerCount { get; set; }
    }

    public class EditUserViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}