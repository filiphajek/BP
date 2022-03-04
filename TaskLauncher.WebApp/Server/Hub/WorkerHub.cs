using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.WebApp.Server.Hub;

public interface IWorkerHub
{
    /// <summary>
    /// Tento endpoint vola uzivatel a posloucha na nem launcher
    /// Uzivatel spousti vypocet
    /// </summary>
    Task StartTask(TaskModel model);

    /// <summary>
    /// Na tomto endpointu posloucha uzivatel a vola ho launcher
    /// Informuje o zmene stavu tasku (zacatek vypoctu, konec apod.)
    /// </summary>
    Task TaskStatusChanged(TaskModel model);
}

[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}, {CookieAuthenticationDefaults.AuthenticationScheme}")]
public class WorkerHub : Hub<IWorkerHub>
{
    private readonly ILogger<WorkerHub> logger;
    private readonly SignalRMemoryStorage storage;

    public WorkerHub(ILogger<WorkerHub> logger, SignalRMemoryStorage storage)
    {
        this.logger = logger;
        this.storage = storage;
    }

    [Authorize]
    public async Task StartTask(TaskModel model)
    {
        if (!Context.User!.TryGetAuth0Id(out var id))
            return;

        model.UserId = id;
        await Clients.Clients(storage.GetConnections("1MBhNBPqfSs8FYlaHoFLe2uRwa5BV5Qa")).StartTask(model);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "launcher")]
    public async Task TaskStatusChanged(TaskModel model)
    {
        await Clients.Clients(storage.GetConnections(model.UserId)).TaskStatusChanged(model);
    }

    public override Task OnConnectedAsync()
    {
        logger.LogInformation($"User connected: '{Context.ConnectionId}'");

        if (Context.User is null)
            return Task.CompletedTask;

        //registrace launcher aplikace, v budoucnu toto bude jinak
        var azp = Context.User.Claims.SingleOrDefault(i => i.Type == "azp");
        if (azp?.Value == "1MBhNBPqfSs8FYlaHoFLe2uRwa5BV5Qa")
        {
            storage.Add("1MBhNBPqfSs8FYlaHoFLe2uRwa5BV5Qa", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        if (!Context.User.TryGetAuth0Id(out var id))
        {
            return Task.CompletedTask;
        }

        storage.Add(id, Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Context.User!.TryGetAuth0Id(out var id);
        storage.Remove(id, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}


//userhub pro uzivatelske notifikace

public interface IUserHub 
{

}


public class UserHub : Hub<IUserHub>
{

}