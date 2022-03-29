using Auth0.AuthenticationApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Authorization.Requirements;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.Authorization.Handlers;

public class UserEmailVerifiedHandler : AuthorizationHandler<UserEmailVerifiedRequirement>
{
    private readonly IClientFactory<AuthenticationApiClient> authClientFactory;
    private readonly IHttpContextAccessor httpContextAccessor;

    public UserEmailVerifiedHandler(IClientFactory<AuthenticationApiClient> authClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        this.authClientFactory = authClientFactory;
        this.httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserEmailVerifiedRequirement requirement)
    {
        if (context.User.TryGetClaimAsBool(TaskLauncherClaimTypes.EmailVerified, out var verified) && verified)
        {
            context.Succeed(requirement);
            return;
        }

        if (context.User.TryGetClaimValue("gty", out var gty) && gty == "password")
        {
            var auth0client = await authClientFactory.GetClient();
            var token = httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var userinfo = await auth0client.GetUserInfoAsync(token);
            
            if(userinfo.EmailVerified.HasValue && userinfo.EmailVerified.Value)
                context.Succeed(requirement);
            else
                context.Fail();
            return;
        }
        context.Fail();
    }
}