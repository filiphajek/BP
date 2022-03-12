using Microsoft.AspNetCore.Authorization;
using TaskLauncher.Authorization.Requirements;

namespace TaskLauncher.Authorization.Handlers;

public class NoBanHandler : AuthorizationHandler<NoBanRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NoBanRequirement requirement)
    {
        var ban = context.User.Claims.SingleOrDefault(i => i.Type == "banid");
        if (ban is null)
            context.Succeed(requirement);
        else
            context.Fail();
        return Task.CompletedTask;
    }
}
