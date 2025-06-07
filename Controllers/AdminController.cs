using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using OnlineMovieTicket.Models;
using OnlineMovieTicket.Models.ViewModels;
using OnlineMovieTicket.Services;
using OnlineMovieTicket.Attributes;
using Microsoft.EntityFrameworkCore;

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

        // Admin Dashboard
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // User Management List
        public async Task<IActionResult> UserManagement()
        {
            try
            {
                var users = await _userManager.Users
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.FullName)
                    .ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading users.";
                return View(new List<ApplicationUser>());
            }
        }

        // GET: Edit User
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

            // Prepare ViewBag data
            ViewBag.Roles = Enum.GetValues<UserRole>().Cast<UserRole>().ToList();
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };

            return View(model);
        }

        // POST: Edit User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string fullName, string phoneNumber,
            string gender, DateTime dateOfBirth, string address, UserRole role, bool emailConfirmed = false)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid user ID.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    TempData["ErrorMessage"] = "Full Name is required.";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                if (string.IsNullOrWhiteSpace(gender))
                {
                    TempData["ErrorMessage"] = "Gender is required.";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                if (dateOfBirth == default(DateTime))
                {
                    TempData["ErrorMessage"] = "Date of Birth is required.";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Prevent admin from changing their own role to non-admin
                var currentUserId = _userManager.GetUserId(User);
                if (user.Id == currentUserId && role != UserRole.Admin)
                {
                    TempData["ErrorMessage"] = "You cannot change your own role from Admin.";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                // Store original role for comparison
                var originalRole = user.Role;

                // Update user properties (Email is not changeable)
                user.FullName = fullName.Trim();
                user.PhoneNumber = phoneNumber?.Trim();
                user.Gender = gender;
                user.DateOfBirth = dateOfBirth;
                user.Address = address?.Trim();
                user.EmailConfirmed = emailConfirmed;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = currentUserId;

                // Update user in database
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to update user: {errors}";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                // Update role if changed
                if (originalRole != role)
                {
                    var roleSuccess = await _roleService.AssignRoleToUserAsync(user.Id, role, currentUserId);
                    if (!roleSuccess)
                    {
                        TempData["ErrorMessage"] = "User information updated but failed to update role.";
                        return RedirectToAction(nameof(EditUser), new { id });
                    }
                }

                TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully.";
                return RedirectToAction(nameof(UserManagement));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the user.";
                return RedirectToAction(nameof(EditUser), new { id });
            }
        }

        // POST: Reset Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            try
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

                // Generate new random password
                var newPassword = GenerateRandomPassword();
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    TempData["ResetPasswordSuccess"] = $"Password reset successfully. New password: {newPassword}";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to reset password: {errors}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while resetting password.";
            }

            return RedirectToAction(nameof(EditUser), new { id });
        }

        // POST: Delete User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
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

                // Prevent deleting other admins for security
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
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to delete user: {errors}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // GET: Create User
        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewBag.Roles = Enum.GetValues<UserRole>().Cast<UserRole>().ToList();
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            return View();
        }

        // POST: Create User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string fullName, string email, string phoneNumber,
            string gender, DateTime dateOfBirth, string address, UserRole role, bool emailConfirmed = false)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    TempData["ErrorMessage"] = "Full Name is required.";
                    return RedirectToAction(nameof(CreateUser));
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["ErrorMessage"] = "Email is required.";
                    return RedirectToAction(nameof(CreateUser));
                }

                if (string.IsNullOrWhiteSpace(gender))
                {
                    TempData["ErrorMessage"] = "Gender is required.";
                    return RedirectToAction(nameof(CreateUser));
                }

                if (dateOfBirth == default(DateTime))
                {
                    TempData["ErrorMessage"] = "Date of Birth is required.";
                    return RedirectToAction(nameof(CreateUser));
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "Email address is already in use.";
                    return RedirectToAction(nameof(CreateUser));
                }

                // Create new user
                var currentUserId = _userManager.GetUserId(User);
                var newUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName.Trim(),
                    PhoneNumber = phoneNumber?.Trim(),
                    Gender = gender,
                    DateOfBirth = dateOfBirth,
                    Address = address?.Trim(),
                    Role = role,
                    EmailConfirmed = emailConfirmed,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };

                // Generate temporary password
                var tempPassword = GenerateRandomPassword();
                var result = await _userManager.CreateAsync(newUser, tempPassword);

                if (result.Succeeded)
                {
                    // Assign role
                    var roleSuccess = await _roleService.AssignRoleToUserAsync(newUser.Id, role, currentUserId);
                    if (roleSuccess)
                    {
                        TempData["SuccessMessage"] = $"User '{newUser.FullName}' created successfully. Temporary password: {tempPassword}";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"User '{newUser.FullName}' created but failed to assign role. Temporary password: {tempPassword}";
                    }
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to create user: {errors}";
                    return RedirectToAction(nameof(CreateUser));
                }

                return RedirectToAction(nameof(UserManagement));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the user.";
                return RedirectToAction(nameof(CreateUser));
            }
        }

        // Helper method to generate random password
        private string GenerateRandomPassword()
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";

            var random = new Random();
            var password = new char[8];

            // Ensure at least one character from each category
            password[0] = lowercase[random.Next(lowercase.Length)];
            password[1] = uppercase[random.Next(uppercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            // Fill the rest with random characters from all categories
            var allChars = lowercase + uppercase + digits + special;
            for (int i = 4; i < 8; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle the array
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                char temp = password[i];
                password[i] = password[j];
                password[j] = temp;
            }

            return new string(password);
        }
    }
}