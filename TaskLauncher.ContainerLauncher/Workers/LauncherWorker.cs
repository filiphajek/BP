using IdentityModel.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;
using TaskLauncher.Common.Services;
using TaskLauncher.ContainerLauncher.Extensions;

namespace TaskLauncher.ContainerLauncher.Workers;

public class LauncherWorker : BackgroundService
{
    private readonly ConcurrentQueue<TaskModel> tasks = new(); // fronta tasku
    private TaskCompletionSource<bool> tmpCompletionSource = new();
    private readonly ILogger<LauncherWorker> logger;
    private readonly ITaskLauncherService launcher;
    private readonly SignalRClient signalrClient;
    private readonly HttpClient client;
    private readonly TokenProvider provider;
    private readonly string folderPath;

    public LauncherWorker(ILogger<LauncherWorker> logger, ITaskLauncherService launcher, 
        SignalRClient signalrClient, HttpClient client, TokenProvider provider, TaskLauncherConfig taskLauncherConfig)
    {
        this.logger = logger;
        this.launcher = launcher;
        this.signalrClient = signalrClient;
        this.client = client;
        this.provider = provider;
        //registrace odchyceni zpravy
        signalrClient.RegisterOnReceivedTask("StartTask", EnqueueNewTask);
        //pracovni adresar
        Directory.CreateDirectory(taskLauncherConfig.Source);
        folderPath = taskLauncherConfig.Source;
    }

    /// <summary>
    /// Ziskani poradi tasku ve fronte
    /// </summary>
    public int GetIndexOfTask(Guid id)
    {
        var tmp = tasks.SingleOrDefault(i => i.TaskId == id);
        return tasks.IndexOf(tmp);
    }

    /// <summary>
    /// Zarazeni tasku do fronty, probehne akualizace stavu tasku
    /// </summary>
    public async Task EnqueueNewTask(TaskModel model)
    {
        // zamci tuto cast kodu, mohlo by se totiz stat ze fronta je prazdna, prijdou 2 zpravy -> metoda se zavola 2x
        // a stihne se vse pridat do fronty pred podminkou, podminka by se pak nikdy nevykonala
        lock (tasks)
        {
            tasks.Enqueue(model);
            logger.LogInformation($"Task '{model.TaskId}' from user '{model.UserId}' is enqueued");
            if (tasks.Count == 1)
                tmpCompletionSource?.TrySetResult(true);
        }
        await UpdateTaskAsync(model, TaskState.InQueue);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        //ziskani autorizacniho tokenu k web api
        var token = await provider.Authorize();
        client.SetBearerToken(token);

        //pripojeni na signalr hub
        await signalrClient.TryToConnect(cancellationToken);
        logger.LogInformation("Connected, service starting");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Service is stopping");
        await signalrClient.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if(tasks.IsEmpty)
            {
                //pokud je fronta prazdna, cekej na dalsi prichozi zpravu
                logger.LogInformation("Task queue is empty, waiting for next one");
                await tmpCompletionSource.Task;
                tmpCompletionSource = new();
                continue; // pro jistotu, muze nastat napriklad to ze frontNotEmptyCompletionSource se nastavilo na true ale fronta je porad prazdna
            }

            bool tmp = tasks.TryDequeue(out var taskModel);
            if (tmp && taskModel is not null)
            {
                await TaskExecution(taskModel);
            }
        }
    }

    /// <summary>
    /// Proces spusteni kontejneru a cekani az se prace dokonci
    /// </summary>
    private async Task TaskExecution(TaskModel model)
    {
        logger.LogInformation($"Starting execution of task '{model.TaskId}'");

        //pomoci httpclienta stahni soubor
        var file = await client.GetAsync($"launcher/task?userId={model.UserId}&id={model.TaskId}");
        File.WriteAllText(Path.Combine(folderPath, "task.txt"), await file.Content.ReadAsStringAsync());

        //poslani informace o pripraveni tasku k spusteni
        await UpdateTaskAsync(model, TaskState.Prepared);
        
        //start kontejneru
        var tmp = await launcher.StartContainer();

        //poslani informace o behu kontejneru
        await UpdateTaskAsync(model, TaskState.Running);

        //cekani az se dokonci task
        await launcher.WaitContainer(tmp.ContainerId);

        //poslat na api soubor s vysledkem
        using (var stream = new StreamWriter(File.Open(Path.Combine(folderPath, "task.txt"), FileMode.Create)))
        {
            await stream.WriteLineAsync("some simulated result");
        }
        using (var resultFile = File.Open(Path.Combine(folderPath, "task.txt"), FileMode.Open))
        {
            await client.SendMultipartFormDataAsync($"launcher/task?userId={model.UserId}&id={model.TaskId}", resultFile);
        }
        await UpdateTaskAsync(model, TaskState.Finished);
        logger.LogInformation($"Task '{model.TaskId}' finished");
    }

    /// <summary>
    /// Aktualiazace databaze a poslani zpravy klientovi
    /// </summary>
    private async Task UpdateTaskAsync(TaskModel model, TaskState state)
    {
        model.State = state;
        model.Time = DateTime.Now;
        await client.PutAsJsonAsync("launcher/task", new TaskStatusUpdateRequest { Id = model.TaskId, State = model.State, Time = model.Time });
        await signalrClient.Connection.InvokeTaskStatusChanged(model);
    }
}
