using Microsoft.AspNetCore.Authorization;
using TaskLauncher.Authorization.Requirements;

namespace TaskLauncher.Authorization.Handlers;

public class CanCancelHandler : AuthorizationHandler<CanCancelRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanCancelRequirement requirement)
    {
        if (context.User.IsInRole("admin"))
        {
            context.Fail();
            return Task.CompletedTask;
        }
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
