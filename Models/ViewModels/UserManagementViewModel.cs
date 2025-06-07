using System.ComponentModel.DataAnnotations;
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

        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [Display(Name = "Address")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Role")]
        public UserRole Role { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        // Read-only properties for system information
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