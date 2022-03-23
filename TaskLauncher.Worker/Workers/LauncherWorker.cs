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

public class LauncherWorker : BackgroundService
{
    private readonly ITaskLauncherService launcher;
    private readonly HttpClient httpClient;
    private readonly SignalRClient signalrClient;
    private readonly ILogger<LauncherWorker> logger;
    private readonly IFileStorageService fileService;
    private readonly ManagementTokenService managementTokenService;

    private TaskCompletionSource<bool> tmpCompletionSource = new();
    private CancellationTokenSource? tokenSource = null;
    private TaskModel? actualTask = null;
    private bool isWorking = false;

    public LauncherWorker(ILogger<LauncherWorker> logger,
        IFileStorageService fileService,
        ManagementTokenService managementTokenService,
        ITaskLauncherService launcher,
        IOptions<ServiceAddresses> serviceAddresses,
        SignalRClient signalrClient)
    {
        this.logger = logger;
        this.fileService = fileService;
        this.managementTokenService = managementTokenService;
        this.launcher = launcher;
        httpClient = new HttpClient { BaseAddress = serviceAddresses.Value.WebApiAddressUri };
        this.signalrClient = signalrClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //ziskani autorizacniho tokenu k web api
        var token = await managementTokenService.GetApiToken(new(), "task-api", false);
        httpClient.SetBearerToken(token);

        var config = await httpClient.GetFromJsonAsync<ConfigResponse>("api/config/worker?key=tasktimeout", stoppingToken);
        var timeout = 40;
        if (config is not null)
            _ = int.TryParse(config.Value, out timeout);

        tokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        tokenSource.CancelAfter(TimeSpan.FromMinutes(timeout));

        signalrClient.RegisterOnReceivedTask(async i =>
        {
            try
            {
                await TaskExecution(i, tokenSource.Token);
            }
            catch(OperationCanceledException ex)
            {
                logger.LogError("Task '{0}' timeouted", actualTask.Id);
                actualTask.State = TaskState.Timeouted;
                await signalrClient.Connection.InvokeTaskTimeouted(actualTask);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        });
        signalrClient.RegisterOnCancelTask(async i =>
        {
            if (actualTask is not null && i.Id == actualTask.Id)
            {
                actualTask.State = TaskState.Cancelled;
                await signalrClient.Connection.InvokeRequestWork();
                tokenSource.Cancel();
            }
        });
        signalrClient.Connection.OnWakeUpMessage(async () =>
        {
            if (!isWorking)
            {
                logger.LogInformation("is working?");
                await signalrClient.Connection.InvokeRequestWork();
            }
        });

        //pripojeni na signalr hub
        await signalrClient.TryToConnect(tokenSource.Token);
        logger.LogInformation("Connected, worker is starting");

        //hlavni smycka
        while (true)
        {
            tokenSource.Token.ThrowIfCancellationRequested();
            await tmpCompletionSource.Task;
            tmpCompletionSource = new();
        }
    }

    private async Task TaskExecution(TaskModel model, CancellationToken token)
    {
        isWorking = true;
        actualTask = model;
        logger.LogInformation("Starting execution of task '{0}'", model.Id);

        //stazeni souboru
        using (var file = File.Create("task.txt")) //TODO volume mount
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
        await launcher.WaitContainer(tmp.ContainerId, token);

        //upload souboru s vysledkem
        using (var resultFile = File.Open("task.txt", FileMode.Open))
        {
            await fileService.UploadFileAsync(model.ResultFilePath, resultFile, token);
        }
     
        //ukonceni prace
        isWorking = false;
        await UpdateTaskAsync(model, TaskState.FinishedSuccess, token);
        logger.LogInformation("Task '{0}' finished", model.Id);
    }

    private async Task UpdateTaskAsync(TaskModel model, TaskState state, CancellationToken cancellationToken = default)
    {
        model.State = state;
        model.Time = DateTime.Now;
        await signalrClient.Connection.InvokeTaskStatusChanged(model);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Service is stopping");
        await signalrClient.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
