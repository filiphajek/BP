using Microsoft.AspNetCore.SignalR.Client;
using TaskLauncher.App.Client.Extensions;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Client;

/// <summary>
/// SignalR klient, implementace uklidu vsech IDisposable
/// </summary>
public class SignalRClient : IAsyncDisposable
{
    //signalr spojeni
    public HubConnection Connection { get; }

    //vsechny registrace na odchytavani real-time zprav
    private readonly HashSet<IDisposable> registrations = new();

    //pokusy pro prihlaseni na signakr hub
    private int attemps = 0;
    public int Attemps { get; init; } = 15;

    public event Action<TaskModel> OnTaskUpdate;

    public SignalRClient(ServiceAddresses serviceAddresses)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(serviceAddresses.HubAddress)
            .WithAutomaticReconnect()
            .Build();
    }

    /// <summary>
    /// Registrace odchyceni zpravy o spusteni task
    /// </summary>
    public void RegisterOnTaskUpdate(Action<TaskModel> handler)
    {
        var tmp = Connection.OnNotification(i =>
        {
            if (OnTaskUpdate is not null)
            {
                OnTaskUpdate.Invoke(i);
                return;
            }
            handler.Invoke(i);
        });
        registrations.Add(tmp);
    }

    /// <summary>
    /// Pripojeni na hub
    /// </summary>
    public async Task TryToConnect(CancellationToken cancellationToken = default)
    {
        while (Attemps != attemps)
        {
            try
            {
                await Connection.StartAsync(cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                attemps++;
            }
            await Task.Delay(3000, cancellationToken);
        }
        throw new TimeoutException();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in registrations)
        {
            disposable.Dispose();
        }

        if (Connection is not null)
        {
            await Connection.DisposeAsync();
        }
    }
}