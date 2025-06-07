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
                // Admin có tất cả quyền
                "ManageUsers" => userRole == UserRole.Admin,
                "ManageWebsite" => userRole == UserRole.Admin,
                "ManageSystemSettings" => userRole == UserRole.Admin,

                // Manager chỉ có quyền liên quan đến rạp phim
                "ManageMovies" => userRole == UserRole.Admin || userRole == UserRole.Manager,
                "ManageShowTimes" => userRole == UserRole.Admin || userRole == UserRole.Manager,
                "ViewBookings" => userRole == UserRole.Admin || userRole == UserRole.Manager,
                "ManageTheaters" => userRole == UserRole.Admin || userRole == UserRole.Manager,
                "ViewCinemaReports" => userRole == UserRole.Admin || userRole == UserRole.Manager,

                // Tất cả user có thể đặt vé
                "BookTickets" => true,

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

            // Chỉ Admin mới có thể quản lý users
            if (managerRole == UserRole.Admin) return true;

            // Manager KHÔNG thể quản lý users
            return false;
        }
    }
}