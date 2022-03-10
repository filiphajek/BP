using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.WebApp.Server.Hub;

public interface IUserHub
{
    void Notify(TaskModel model);
}

//mohu pouzit jako IHubContext klidne i v jinym hubu protoze to je singleton !!
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class UserHub : Hub<IUserHub>
{
    private readonly SignalRMemoryStorage userConnectionsStorage;

    public UserHub(SignalRMemoryStorage userConnectionsStorage)
    {
        this.userConnectionsStorage = userConnectionsStorage;
    }

    public override Task OnConnectedAsync()
    {
        if (Context.User is null || !Context.User.TryGetAuth0Id(out var id))
        {
            Context.Abort();
            return Task.CompletedTask;
        }

        userConnectionsStorage.Add(id, Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Context.User!.TryGetAuth0Id(out var id);
        userConnectionsStorage.Remove(id, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}