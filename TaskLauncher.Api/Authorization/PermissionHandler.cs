using Microsoft.AspNetCore.Authorization;

namespace TaskLauncher.Api.Authorization;

public class PermissionHandler<TRequirement> : AuthorizationHandler<TRequirement> where TRequirement : IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (requirement == null)
            throw new ArgumentNullException(nameof(requirement));

        //todo lze udelat pres RequireClaim
        var permissionClaims = context.User.Claims.Where(t => t.Type == "permissions").Select(i => i.Value);

        foreach (var claim in permissionClaims)
        {
            if(claim == requirement.ToString())
            {
                context.Succeed(requirement);
                break;
            }
        }
        return Task.CompletedTask;
    }
}
