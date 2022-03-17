using Microsoft.AspNetCore.SignalR.Client;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Client.Extensions;

public static class HubConnectionExtension
{
    public static IDisposable OnNotification(this HubConnection connection, Func<TaskModel, Task> handler)
        => connection.On("Notify", handler);

    public static IDisposable OnNotification(this HubConnection connection, Action<TaskModel> handler)
        => connection.On("Notify", handler);

    public static IDisposable OnNewEvent(this HubConnection connection, Action<EventModel> handler)
        => connection.On("SendEvent", handler);
}

public static class HttpClientFactoryExtesnions
{
    public static HttpClient CreateApiClient(this IHttpClientFactory factory)
    {
        return factory.CreateClient("apiclient");
    }
}