using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineMovieTicket.Data;
using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Services
{
    public class RoleService : IRoleService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public RoleService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<bool> AssignRoleToUserAsync(string userId, UserRole role, string assignedBy)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Update custom Role property
            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = assignedBy;

            // Remove from all existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Add to new role
            var roleName = role.ToString();

            // Ensure role exists in AspNetRoles
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
            var updateUserResult = await _userManager.UpdateAsync(user);

            return addToRoleResult.Succeeded && updateUserResult.Succeeded;
        }

        public async Task<UserRole> GetUserRoleAsync(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user?.Role ?? UserRole.User;
        }

        public async Task<bool> HasPermissionAsync(string userId, string permission)
        {
            var userRole = await GetUserRoleAsync(userId);

            return permission switch
            {
                "ManageUsers" => userRole == UserRole.Admin,
                "ManageWebsite" => userRole == UserRole.Admin,
                "ManageTheaters" => userRole == UserRole.Admin || userRole == UserRole.Manager,
                "ManageMovies" => userRole == UserRole.Admin || userRole == UserRole.Manager,
                "ManageBookings" => userRole == UserRole.Admin || userRole == UserRole.Manager,
                "ViewReports" => userRole == UserRole.Admin || userRole == UserRole.Manager,
                "BookTickets" => true, // All authenticated users can book tickets
                _ => false
            };
        }

        public async Task<List<ApplicationUser>> GetUsersByRoleAsync(UserRole role)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<bool> CanManageUserAsync(string managerId, string targetUserId)
        {
            var managerRole = await GetUserRoleAsync(managerId);
            var targetUserRole = await GetUserRoleAsync(targetUserId);

            // Admin can manage everyone
            if (managerRole == UserRole.Admin) return true;

            // Manager can only manage regular users, not other managers or admins
            if (managerRole == UserRole.Manager && targetUserRole == UserRole.User) return true;

            return false;
        }
    }
}