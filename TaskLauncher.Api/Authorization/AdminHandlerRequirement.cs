using Microsoft.AspNetCore.Authorization;

namespace TaskLauncher.Api.Authorization;

public sealed class AdminHandlerRequirement : IAuthorizationRequirement 
{
    public override string ToString() => "add:token";
}

public sealed class UserHandlerRequirement : IAuthorizationRequirement
{
    public override string ToString() => "crud:task";
}