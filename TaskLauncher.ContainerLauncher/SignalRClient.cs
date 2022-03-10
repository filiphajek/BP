using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using TaskLauncher.Common.Auth0;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Messages;
using TaskLauncher.Common.Models;
using TaskLauncher.Common.Services;
using TaskLauncher.ContainerLauncher.Extensions;

namespace TaskLauncher.ContainerLauncher;

/// <summary>
/// SignalR klient, implementace uklidu vsech IDisposable
/// </summary>
public class SignalRClient : IAsyncDisposable
{
    //signalr spojeni
    public HubConnection Connection { get; }
    
    //vsechny registrace na odchytavani real-time zprav
    private readonly HashSet<IDisposable> registrations = new();

    //pokusy pro prihlaseni na signalr hub
    private int attemps = 0;
    public int Attemps { get; init; } = 15;

    public SignalRClient(IOptions<ServiceAddresses> serviceAddresses, ManagementTokenService tokenService)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(serviceAddresses.Value.HubAddress, options =>
            {
                //ziskani autorizacniho tokenu pro pristup na signalr hub
                options.AccessTokenProvider = async () => await tokenService.GetApiToken(new(), "task-api", false);
                //pouze pro testovani, kdy nemam validni certifikat
                options.HttpMessageHandlerFactory = (x) => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
            })
            .WithAutomaticReconnect()
            .Build();
    }

    /// <summary>
    /// Registrace odchyceni zpravy o spusteni task
    /// </summary>
    public void RegisterOnReceivedTask(Func<TaskModel, Task> handler)
    {
        var tmp = Connection.OnTaskStarted(handler);
        registrations.Add(tmp);
    }

    /// <summary>
    /// Registrace odchyceni zpravy o zruseni tasku
    /// </summary>
    public void RegisterOnCancelTask(Action<TaskModel> handler)
    {
        var tmp = Connection.OnCancelTask(handler);
        registrations.Add(tmp);
    }

    /// <summary>
    /// Pripojeni na hub
    /// </summary>
    public async Task TryToConnect(CancellationToken cancellationToken)
    {
        while (Attemps != attemps)
        {
            try
            {
                await Connection.StartAsync(cancellationToken);
                return;
            }
            catch(Exception ex)
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
