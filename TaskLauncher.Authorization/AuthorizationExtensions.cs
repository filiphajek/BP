﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TaskLauncher.Authorization.Handlers;
using TaskLauncher.Authorization.Requirements;

namespace TaskLauncher.Authorization;

public static class AuthorizationExtensions
{
    public static void AddAuthorizationHandlers<TAssembly>(this IServiceCollection services)
    {
        var handlers = typeof(TAssembly).Assembly.ExportedTypes
            .Where(type => type.IsAssignableTo(typeof(AuthorizationHandler<>)))
            .Where(type => type.IsSealed);
        foreach (var handler in handlers)
        {
            services.AddSingleton(typeof(IAuthorizationHandler), handler);
        }
    }

    public static void AddAuthorizationClient(this IServiceCollection services)
    {
        services.AddAuthorizationHandlers<CanCancelHandler>();
        services.AddAuthorizationCore(policies =>
        {
            policies.AddPolicy(TaskLauncherPolicies.CanCancelAccount, p =>
            {
                p.Requirements.Add(new CanCancelRequirement());
            });
            policies.AddPolicy(TaskLauncherPolicies.EmailNotConfirmed, p =>
            {
                p.RequireClaim(TaskLauncherClaimTypes.EmailVerified, "false");
            });
            policies.AddPolicy(TaskLauncherPolicies.UserNotRegistered, p =>
            {
                p.RequireClaim(TaskLauncherClaimTypes.Registered, "false");
            });
            policies.AddPolicy(TaskLauncherPolicies.UserPolicy, p =>
            {
                p.RequireRole(TaskLauncherRoles.User, TaskLauncherRoles.Admin);
                p.RequireClaim(TaskLauncherClaimTypes.Registered, "true");
                p.RequireClaim(TaskLauncherClaimTypes.EmailVerified, "true");
            });
            policies.AddPolicy(TaskLauncherPolicies.AdminPolicy, p =>
            {
                p.RequireRole(TaskLauncherRoles.Admin);
                p.RequireClaim(TaskLauncherClaimTypes.Registered, "true");
                p.RequireClaim(TaskLauncherClaimTypes.EmailVerified, "true");
            });
        });
    }

    public static void AddAuthorizationServer(this IServiceCollection services)
    {
        services.AddAuthorizationHandlers<CanCancelHandler>();
        services.AddAuthorizationCore(policies =>
        {
            policies.AddPolicy(TaskLauncherPolicies.CanCancelAccount, p =>
            {
                p.Requirements.Add(new CanCancelRequirement());
            });
            policies.AddPolicy(TaskLauncherPolicies.EmailNotConfirmed, p =>
            {
                p.RequireClaim(TaskLauncherClaimTypes.EmailVerified, "false");
            });
            policies.AddPolicy(TaskLauncherPolicies.UserNotRegistered, p =>
            {
                p.RequireClaim(TaskLauncherClaimTypes.Registered, "false");
            });
            policies.AddPolicy(TaskLauncherPolicies.AdminPolicy, p =>
            {
                p.AddAuthenticationSchemes("Cookies", "Bearer");
                p.RequireClaim(TaskLauncherClaimTypes.Registered, "true");
                p.RequireClaim(TaskLauncherClaimTypes.EmailVerified, "true");
                p.RequireRole(TaskLauncherRoles.Admin);
            });
            policies.AddPolicy(TaskLauncherPolicies.UserPolicy, p =>
            {
                p.AddAuthenticationSchemes(AuthorizationConstants.CookieAuth, AuthorizationConstants.BearerAuth);
                p.RequireClaim(TaskLauncherClaimTypes.Registered, "true");
                p.RequireClaim(TaskLauncherClaimTypes.EmailVerified, "true");
                p.RequireRole(TaskLauncherRoles.User, TaskLauncherRoles.Admin);
            });
            policies.AddPolicy("launcher", p =>
            {
                p.AddAuthenticationSchemes(AuthorizationConstants.BearerAuth);
                p.RequireClaim("azp", "1MBhNBPqfSs8FYlaHoFLe2uRwa5BV5Qa");
                p.RequireClaim("gty", "client-credentials");
            });
        });
    }
}