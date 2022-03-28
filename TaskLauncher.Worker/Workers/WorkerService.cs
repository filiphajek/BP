using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Services;
using IdentityModel.Client;
using TaskLauncher.Common.Models;
using Microsoft.Extensions.Options;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Worker.Extensions;
using TaskLauncher.Worker.Services;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Responses;

namespace TaskLauncher.Worker.Workers;

/// <summary>
/// Implementace workera jako BackgroundService
/// </summary>
public class WorkerService : BackgroundService
{
    private readonly ITaskLauncherService launcher;
    private readonly HttpClient httpClient;
    private readonly SignalRClient signalrClient;
    private readonly ILogger<WorkerService> logger;
    private readonly IFileStorageService fileService;
    private readonly ManagementTokenService managementTokenService;
    private readonly TaskLauncherConfig config;

    private TaskCompletionSource<bool> tmpCompletionSource = new();
    private TaskModel? actualTask = null;
    private bool isWorking = false;

    public WorkerService(ILogger<WorkerService> logger,
        IFileStorageService fileService,
        IOptions<TaskLauncherConfig> config,
        ManagementTokenService managementTokenService,
        ITaskLauncherService launcher,
        IOptions<ServiceAddresses> serviceAddresses,
        SignalRClient signalrClient)
    {
        this.config = config.Value;
        this.logger = logger;
        this.fileService = fileService;
        this.managementTokenService = managementTokenService;
        this.launcher = launcher;
        HttpClientHandler handler = new();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        httpClient = new HttpClient(handler) { BaseAddress = serviceAddresses.Value.WebApiAddressUri };
        this.signalrClient = signalrClient;
    }

    /// <summary>
    /// Inicializace http klienta, ceka se dokud nepobezi server
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        //cekani na server
        await WaitForServerStartAsync(cancellationToken);

        //ziskani autorizacniho tokenu k web api
        var token = await managementTokenService.GetApiToken(new(), "task-api", false);
        httpClient.SetBearerToken(token);
        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Hlavni funkce sluzby
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //registrace akce na prijeti tasku
        signalrClient.RegisterOnReceivedTask(async i =>
        {
            //ziskani hodnoty timeoutu
            var timeout = await GetTimeoutValue(stoppingToken);
            
            //nastaveni tokensource, ukonci task pokud nestihne do zadane doby vykonat
            var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutTokenSource.CancelAfter(TimeSpan.FromMinutes(timeout));
            try
            {
                //vykonani tasku
                await TaskExecution(i, timeoutTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                if (actualTask is null)
                    return;

                //task se nestihl dodelat nebo se zacyklil
                logger.LogError("Task '{0}' timeouted", actualTask.Id);
                actualTask.State = TaskState.Timeouted;
                await signalrClient.Connection.InvokeTaskTimeouted(actualTask);
            }
            catch (Exception ex)
            {
                //necekana vyjimka, task spadl
                logger.LogError(ex.ToString());
                if (actualTask is not null)
                {
                    actualTask.State = TaskState.Timeouted;
                    await signalrClient.Connection.InvokeTaskCrashed(actualTask);
                }
            }
            finally
            {
                timeoutTokenSource.Dispose();
                timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                logger.LogInformation("xdd");
            }
        });
        
        //registrace akce na wake up zpravu
        signalrClient.Connection.OnWakeUpMessage(async () =>
        {
            if (!isWorking)
            {
                logger.LogInformation("Worker is active again");
                await signalrClient.Connection.InvokeRequestWork();
            }
        });

        //pripojeni na signalr hub
        await signalrClient.TryToConnect(stoppingToken);
        logger.LogInformation("Connected, worker is starting");

        //smycka
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();
            await tmpCompletionSource.Task;
            tmpCompletionSource = new();
        }
    }

    /// <summary>
    /// Funkce ktera vykonava dany task, spousti kontejner se souborem a aktualizuje stav tasku
    /// </summary>
    private async Task TaskExecution(TaskModel model, CancellationToken token)
    {
        isWorking = true;
        actualTask = model;
        logger.LogInformation("Starting execution of task '{0}'", model.Id);

        if(!config.Target.StartsWith("/"))
            config.Target = "/" + config.Target;

        //stazeni souboru
        using (var file = File.Create(Path.Combine(config.Target, "task.txt")))
        {
            await fileService.DownloadFileAsync(model.TaskFilePath, file, token);
        }

        //poslani informace o pripraveni tasku k spusteni
        await UpdateTaskAsync(model, TaskState.Ready, token);

        //kontrola zda neni task zrusen
        token.ThrowIfCancellationRequested();

        //start kontejneru
        var tmp = await launcher.StartContainer(token);

        //poslani informace o behu kontejneru
        await UpdateTaskAsync(model, TaskState.Running, token);

        //cekani az se dokonci task
        var exitCode = await launcher.WaitContainer(tmp.ContainerId, token);

        //upload souboru s vysledkem
        using (var resultFile = File.Open(Path.Combine(config.Target, "task.txt"), FileMode.Open))
        {
            await fileService.UploadFileAsync(model.ResultFilePath, resultFile, token);
        }

        //ukonceni prace
        actualTask = null;
        isWorking = false;
        logger.LogInformation("Task '{0}' finished", model.Id);
        await UpdateTaskAsync(model, exitCode == 0 ? TaskState.FinishedFailure : TaskState.FinishedSuccess, token);
    }

    /// <summary>
    /// Metoda pro zistani aktualni hodnoty timeoutu
    /// </summary>
    private async Task<int> GetTimeoutValue(CancellationToken cancellationToken)
    {
        var config = await httpClient.GetFromJsonAsync<ConfigResponse>("api/config/worker?key=tasktimeout", cancellationToken);
        var timeout = 40;
        if (config is not null)
            _ = int.TryParse(config.Value, out timeout);
        return timeout;
    }

    /// <summary>
    /// Aktualizace stavu tasku
    /// </summary>
    private async Task UpdateTaskAsync(TaskModel model, TaskState state, CancellationToken cancellationToken = default)
    {
        model.State = state;
        model.Time = DateTime.Now;
        await signalrClient.Connection.InvokeTaskStatusChanged(model);
    }

    /// <summary>
    /// Ukonceni spojeni
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Service is stopping");
        await signalrClient.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Cekani na server
    /// </summary>
    private async Task WaitForServerStartAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("health", cancellationToken);
        while (!response.IsSuccessStatusCode)
        {
            response = await httpClient.GetAsync("health", cancellationToken);
            logger.LogInformation("Waiting for server");
            await Task.Delay(5, cancellationToken);
        }
        logger.LogInformation("Server live");
    }
}
