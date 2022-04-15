using Microsoft.AspNetCore.Http;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.App.DAL;

/// <summary>
/// Poskytuje userid
/// </summary>
public interface IUserIdProvider
{
    string GetUserId();
}

/// <summary>
/// Implementace IUserIdProvider, ziskava userid z http contextu
/// </summary>
public class UserIdProvider : IUserIdProvider
{
    private readonly IHttpContextAccessor accessor;

    public UserIdProvider(IHttpContextAccessor accessor)
    {
        this.accessor = accessor;
    }

    public string GetUserId()
    {
        if (accessor.HttpContext is null)
            return string.Empty;
        if (accessor.HttpContext.User is null)
            return string.Empty;

        accessor.HttpContext.User.TryGetAuth0Id(out var userId);
        return userId;
    }
}
