using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Services
{
    public interface IRoleService
    {
        Task<bool> AssignRoleToUserAsync(string userId, UserRole role, string assignedBy);
        Task<UserRole> GetUserRoleAsync(string userId);
        Task<bool> HasPermissionAsync(string userId, string permission);
        Task<List<ApplicationUser>> GetUsersByRoleAsync(UserRole role);
        Task<bool> CanManageUserAsync(string managerId, string targetUserId);
    }
}