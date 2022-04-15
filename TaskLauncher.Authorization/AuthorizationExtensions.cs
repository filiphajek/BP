using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskLauncher.Authorization.Handlers;
using TaskLauncher.Authorization.Requirements;
using TaskLauncher.Common;

namespace TaskLauncher.Authorization;

/// <summary>
/// Pomocne metody pro pridani autorizace na server i klient aplikaci
/// </summary>
public static class AuthorizationExtensions
{
    public static void AddAuthorizationHandlers<TAssembly>(this IServiceCollection services)
    {
        var handlers = typeof(TAssembly).Assembly.ExportedTypes
            .Where(type => type.IsAssignableTo(typeof(IAuthorizationHandler)));
        foreach (var handler in handlers)
        {
            services.AddSingleton(typeof(IAuthorizationHandler), handler);
        }
    }

    public static void AddAuthorizationClient(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, CanCancelHandler>();
        services.AddAuthorizationCore(policies =>
        {
            policies.AddPolicy(Constants.Policies.CanCancelAccount, p =>
            {
                p.Requirements.Add(new CanCancelRequirement());
            });
            policies.AddPolicy(Constants.Policies.EmailNotConfirmed, p =>
            {
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "false");
            });
            policies.AddPolicy(Constants.Policies.UserNotRegistered, p =>
            {
                p.RequireClaim(Constants.ClaimTypes.Registered, "false");
            });
            policies.AddPolicy(Constants.Policies.UserPolicy, p =>
            {
                p.RequireRole(Constants.Roles.User);
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "true");
            });
            policies.AddPolicy(Constants.Policies.AdminPolicy, p =>
            {
                p.RequireRole(Constants.Roles.Admin);
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "true");
            });
            policies.AddPolicy(Constants.Policies.CanViewTaskPolicy, p =>
            {
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "true");
                p.RequireRole(Constants.Roles.Admin, Constants.Roles.User);
            });
            policies.AddPolicy(Constants.Policies.CanHaveProfilePolicy, p =>
            {
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "true");
                p.RequireRole(Constants.Roles.Admin, Constants.Roles.User);
            });
            policies.AddPolicy(Constants.Policies.CanViewGraphsPolicy, p =>
            {
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "true");
                p.RequireRole(Constants.Roles.Admin, Constants.Roles.User);
            });
        });
    }

    public static void AddAuthorizationServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorizationHandlers<CanCancelHandler>();
        services.AddAuthorizationCore(policies =>
        {
            policies.AddPolicy(Constants.Policies.CanCancelAccount, p =>
            {
                p.AddAuthenticationSchemes(Constants.Authorization.CookieAuth, Constants.Authorization.BearerAuth);
                p.Requirements.Add(new CanCancelRequirement());
            });
            policies.AddPolicy(Constants.Policies.EmailNotConfirmed, p =>
            {
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "false");
            });
            policies.AddPolicy(Constants.Policies.UserNotRegistered, p =>
            {
                p.RequireClaim(Constants.ClaimTypes.Registered, "false");
            });
            policies.AddPolicy(Constants.Policies.AdminPolicy, p =>
            {
                p.AddAuthenticationSchemes(Constants.Authorization.CookieAuth, Constants.Authorization.BearerAuth);
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "true");
                p.RequireRole(Constants.Roles.Admin);
            });
            policies.AddPolicy(Constants.Policies.UserPolicy, p =>
            {
                p.AddAuthenticationSchemes(Constants.Authorization.CookieAuth, Constants.Authorization.BearerAuth);
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.Requirements.Add(new UserEmailVerifiedRequirement()); //kvuli simulovanemu pristupu
                p.RequireRole(Constants.Roles.User);
            });
            policies.AddPolicy(Constants.Policies.WorkerPolicy, p =>
            {
                p.AddAuthenticationSchemes(Constants.Authorization.BearerAuth);
                p.RequireClaim("azp", configuration["ProtectedApiAzp"]);
                p.RequireClaim("gty", "client-credentials");
            });
            policies.AddPolicy(Constants.Policies.CanViewTaskPolicy, p =>
            {
                p.AddAuthenticationSchemes(Constants.Authorization.CookieAuth, Constants.Authorization.BearerAuth);
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "true");
                p.RequireRole(Constants.Roles.Admin, Constants.Roles.User);
            });
            policies.AddPolicy(Constants.Policies.CanHaveProfilePolicy, p =>
            {
                p.AddAuthenticationSchemes(Constants.Authorization.CookieAuth, Constants.Authorization.BearerAuth);
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "true");
                p.RequireRole(Constants.Roles.Admin, Constants.Roles.User);
            });
            policies.AddPolicy(Constants.Policies.CanViewGraphsPolicy, p =>
            {
                p.AddAuthenticationSchemes(Constants.Authorization.CookieAuth, Constants.Authorization.BearerAuth);
                p.RequireClaim(Constants.ClaimTypes.Registered, "true");
                p.RequireClaim(Constants.ClaimTypes.EmailVerified, "true");
                p.RequireRole(Constants.Roles.Admin, Constants.Roles.User);
            });
            policies.AddPolicy(Constants.Policies.CanViewConfigPolicy, p =>
            {
                p.AddAuthenticationSchemes(Constants.Authorization.BearerAuth);
                p.RequireClaim("azp", configuration["ProtectedApiAzp"]);
                p.RequireClaim("gty", "client-credentials");
            });
        });
    }
}
