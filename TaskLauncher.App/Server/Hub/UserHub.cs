using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Hub;

public interface IUserHub
{
    /// <summary>
    /// Oznameni o tom ze skoncil task (Success, Failure, Timeout, Crashed)
    /// </summary>
    Task TaskFinished(TaskModel model);

    /// <summary>
    /// Posila event o zmene tasku
    /// </summary>
    Task SendEvent(EventModel model);
}

/// <summary>
/// Hub pro uzivatele, ziskavaji notifikace o svych ulohach
/// </summary>
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class UserHub : Hub<IUserHub>
{
    private readonly ILogger<UserHub> logger;
    private readonly SignalRMemoryStorage userConnectionsStorage;

    public UserHub(ILogger<UserHub> logger, SignalRMemoryStorage userConnectionsStorage)
    {
        this.logger = logger;
        this.userConnectionsStorage = userConnectionsStorage;
    }

    public override Task OnConnectedAsync()
    {
        if (Context.User is null || !Context.User.TryGetAuth0Id(out var id))
        {
            Context.Abort();
            return Task.CompletedTask;
        }
        logger.LogInformation("User '{0}' connected to hub", id);
        userConnectionsStorage.Add(id, Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Context.User!.TryGetAuth0Id(out var id);
        logger.LogInformation("User '{0}' disconnected from hub", id);
        userConnectionsStorage.Remove(id, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}