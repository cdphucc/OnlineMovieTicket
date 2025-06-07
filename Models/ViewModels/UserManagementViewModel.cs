using OnlineMovieTicket.Models;
using System.ComponentModel.DataAnnotations;

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

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        public string Gender { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public string Address { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        // Read-only properties
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Updated By")]
        public string UpdatedBy { get; set; }
    }
}