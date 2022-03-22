using Microsoft.AspNetCore.Http;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.App.DAL;

public interface IUserIdProvider
{
    string GetUserId();
}


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
