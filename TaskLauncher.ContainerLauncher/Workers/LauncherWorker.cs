using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Messages;
using TaskLauncher.Common.Services;
using TaskLauncher.Common.Auth0;
using IdentityModel.Client;
using TaskLauncher.Api.Contracts.Requests;
using System.Net.Http.Json;

namespace TaskLauncher.ContainerLauncher.Workers;

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
    private TaskCreated? actualTask = null;

    public LauncherWorker(ILogger<LauncherWorker> logger, 
        IFileStorageService fileService,
        ManagementTokenService managementTokenService,
        ITaskLauncherService launcher,
        HttpClient httpClient,
        SignalRClient signalrClient)
    {
        this.logger = logger;
        this.fileService = fileService;
        this.managementTokenService = managementTokenService;
        this.launcher = launcher;
        this.httpClient = httpClient;
        this.signalrClient = signalrClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        tokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        signalrClient.RegisterOnReceivedTask("StartTask", async i => await TaskExecution(i, tokenSource.Token));
        signalrClient.RegisterOnCancelTask("CancelTask", i =>
        {
            if(actualTask is not null && i.TaskId == actualTask.Id)
                tokenSource.Cancel();
        });

        //ziskani autorizacniho tokenu k web api
        var token = await managementTokenService.GetApiToken(httpClient, "task-api", false);
        httpClient.SetBearerToken(token);

        //pripojeni na signalr hub
        await signalrClient.TryToConnect(tokenSource.Token);
        logger.LogInformation("Connected, service starting");

        //hlavni smycka
        while (true)
        {
            tokenSource.Token.ThrowIfCancellationRequested();
            await tmpCompletionSource.Task;
            tmpCompletionSource = new();
        }
    }

    private async Task TaskExecution(TaskCreated model, CancellationToken token)
    {
        actualTask = model;
        logger.LogInformation("Starting execution of task '{0}'", model.Id);
        
        //stazeni souboru
        using (var file = File.Create("tmp/task.txt"))
        {
            await fileService.DownloadFileAsync(model.TaskFilePath, file);
        }

        //poslani informace o pripraveni tasku k spusteni
        await UpdateTaskAsync(model, TaskState.Prepared, token);

        //kontrola zda neni task zrusen
        token.ThrowIfCancellationRequested();

        //start kontejneru
        var tmp = await launcher.StartContainer(token);

        //poslani informace o behu kontejneru
        await UpdateTaskAsync(model, TaskState.Running, token);

        //cekani az se dokonci task
        await launcher.WaitContainer(tmp.ContainerId, token);

        //upload souboru s vysledkem
        using (var resultFile = File.Open("tmp/task.txt", FileMode.Open))
        {
            await fileService.UploadFileAsync(model.ResultFilePath, resultFile);
        }
        await UpdateTaskAsync(model, TaskState.Finished, token);
        logger.LogInformation("Task '{0}' finished", model.Id);

        token.ThrowIfCancellationRequested();
    }

    private async Task UpdateTaskAsync(TaskCreated model, TaskState state, CancellationToken cancellationToken)
    {
        model.State = state;
        model.Time = DateTime.Now;
        await httpClient.PutAsJsonAsync("launcher/task", new TaskStatusUpdateRequest { Id = model.Id, State = model.State, Time = model.Time }, cancellationToken);
        //await signalrClient.Connection.InvokeTaskStatusChanged(model);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Service is stopping");
        await signalrClient.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
