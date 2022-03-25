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
    private readonly ILogger<SignalRClient> logger;

    //pokusy pro prihlaseni na signakr hub
    private int attemps = 0;
    public int Attemps { get; init; } = 15;

    public event Action<TaskModel> OnTaskUpdate;

    public SignalRClient(ServiceAddresses serviceAddresses, ILogger<SignalRClient> logger, ILoggerProvider loggerProvider)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(serviceAddresses.HubAddress)
            .WithAutomaticReconnect()
            .ConfigureLogging(i =>
            {
                i.AddProvider(loggerProvider);
                i.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();
        this.logger = logger;
    }

    /// <summary>
    /// Registrace odchyceni zpravy o spusteni task
    /// </summary>
    public void RegisterOnTaskUpdate(Action<TaskModel> handler)
    {
        var tmp = Connection.OnNotification(i =>
        {
            logger.LogInformation("New task update '{0}'", i.Id);
            OnTaskUpdate?.Invoke(i);
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
        logger.LogInformation("Cant connect. Timeouted. Trying again");
        throw new TimeoutException();
    }

    public async ValueTask DisposeAsync()
    {
        logger.LogInformation("SignalRClient is disposing");
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