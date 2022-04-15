using Microsoft.AspNetCore.Authorization;
using TaskLauncher.Authorization.Requirements;
using TaskLauncher.Common;

namespace TaskLauncher.Authorization.Handlers;

/// <summary>
/// Handler kontrolujici zda uzivatel muze rusit svuj ucet
/// </summary>
public class CanCancelHandler : AuthorizationHandler<CanCancelRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanCancelRequirement requirement)
    {
        if (context.User.IsInRole(Constants.Roles.Admin))
        {
            context.Fail();
            return Task.CompletedTask;
        }
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
