using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using OnlineMovieTicket.Models;
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
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(string id, UserRole role)
        {
            var currentUserId = _userManager.GetUserId(User);
            var success = await _roleService.AssignRoleToUserAsync(id, role, currentUserId);

            if (success)
            {
                TempData["SuccessMessage"] = "User role updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update user role.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(UserManagement));
        }
    }
}