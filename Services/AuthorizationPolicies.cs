using Microsoft.AspNetCore.Authorization;
using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Services
{
    public static class AuthorizationPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string ManagerOrAdmin = "ManagerOrAdmin";
        public const string AllUsers = "AllUsers";

        public static void ConfigureAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(AdminOnly, policy =>
                    policy.RequireAssertion(context =>
                        context.User.IsInRole(UserRole.Admin.ToString())));

                options.AddPolicy(ManagerOrAdmin, policy =>
                    policy.RequireAssertion(context =>
                        context.User.IsInRole(UserRole.Admin.ToString()) ||
                        context.User.IsInRole(UserRole.Manager.ToString())));

                options.AddPolicy(AllUsers, policy =>
                    policy.RequireAuthenticatedUser());
            });
        }
    }
}