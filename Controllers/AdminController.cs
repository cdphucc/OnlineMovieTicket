using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using OnlineMovieTicket.Models;
using OnlineMovieTicket.Services;
using OnlineMovieTicket.Attributes;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OnlineMovieTicket.Controllers
{
    [AuthorizeRole(UserRole.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRoleService _roleService;

        public AdminController(UserManager<ApplicationUser> userManager, IRoleService roleService)
        {
            _userManager = userManager;
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> UserManagement()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid user ID.";
                return RedirectToAction(nameof(UserManagement));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(UserManagement));
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                Role = user.Role,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                CreatedBy = user.CreatedBy,
                UpdatedBy = user.UpdatedBy
            };

            ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
                ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(UserManagement));
            }

            // Prevent admin from changing their own role to non-admin
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId && model.Role != UserRole.Admin)
            {
                TempData["ErrorMessage"] = "You cannot change your own role from Admin.";
                ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
                ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
                return View(model);
            }

            // Check if email is changing and if it's unique
            if (user.Email != model.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "Email is already taken by another user.");
                    ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
                    ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
                    return View(model);
                }
            }

            // Update user properties
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email; // Keep username in sync with email
            user.PhoneNumber = model.PhoneNumber;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;
            user.Address = model.Address;
            user.EmailConfirmed = model.EmailConfirmed;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = currentUserId;

            // Update user in database
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
                ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
                return View(model);
            }

            // Update role if changed
            if (user.Role != model.Role)
            {
                var roleSuccess = await _roleService.AssignRoleToUserAsync(user.Id, model.Role, currentUserId);
                if (!roleSuccess)
                {
                    TempData["ErrorMessage"] = "User information updated but failed to update role.";
                    return RedirectToAction(nameof(UserManagement));
                }
            }

            TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully.";
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid user ID.";
                return RedirectToAction(nameof(UserManagement));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(UserManagement));
            }

            // Prevent admin from deleting themselves
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(UserManagement));
            }

            // Prevent deleting other admins (optional security measure)
            if (user.Role == UserRole.Admin)
            {
                TempData["ErrorMessage"] = "Cannot delete admin users for security reasons.";
                return RedirectToAction(nameof(UserManagement));
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User '{user.FullName}' deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user. Please try again.";
            }

            return RedirectToAction(nameof(UserManagement));
        }
    }

    // ViewModel for EditUser
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
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Updated By")]
        public string UpdatedBy { get; set; }
    }
}