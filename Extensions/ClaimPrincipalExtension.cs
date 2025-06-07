using System.Security.Claims;
using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static UserRole GetUserRole(this ClaimsPrincipal principal)
        {
            var roleClaimValue = principal.FindFirst("UserRole")?.Value;
            if (string.IsNullOrEmpty(roleClaimValue))
            {
                // Fallback to AspNetRoles
                if (principal.IsInRole("Admin"))
                    return UserRole.Admin;
                if (principal.IsInRole("Manager"))
                    return UserRole.Manager;
                return UserRole.User;
            }

            return Enum.TryParse<UserRole>(roleClaimValue, out var role) ? role : UserRole.User;
        }

        public static bool HasRole(this ClaimsPrincipal principal, UserRole role)
        {
            return principal.GetUserRole() == role;
        }

        public static bool HasAnyRole(this ClaimsPrincipal principal, params UserRole[] roles)
        {
            return roles.Contains(principal.GetUserRole());
        }
    }
}