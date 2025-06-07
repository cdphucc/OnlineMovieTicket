using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using OnlineMovieTicket.Models;
using OnlineMovieTicket.Models.ViewModels; // Add this using statement
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
    
    [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Tạo mật khẩu mới ngẫu nhiên
            var newPassword = Guid.NewGuid().ToString().Substring(0, 8);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                TempData["ResetPassword"] = $"Mật khẩu mới: {newPassword}";
                // (Tùy chọn) Gửi mail cho user tại đây
            }
            else
            {
                TempData["ResetPassword"] = "Đặt lại mật khẩu thất bại!";
            }
            return RedirectToAction("EditUser", new { id });
        }
    } 
}