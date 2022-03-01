using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RawRabbit.Common;
using RawRabbit.Extensions.Client;
using RoundRobin;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Messages;
using TaskLauncher.Common.Services;
using TaskLauncher.Common.TypedRawRabbit;
using TaskLauncher.ContainerLauncher.Queue;

namespace TaskLauncher.ContainerLauncher.Workers;

public class LauncherWorker : BackgroundService
{
    private readonly ITaskLauncherService launcher;
    private readonly ILogger<LauncherWorker> logger;
    private readonly IBusClient busClient;
    private readonly IDefaultSubscriber subscriber;
    private readonly IDefaultPublisher publisher;
    private readonly IFileStorageService fileStorageService;
    private readonly QueuesPriorityConfiguration options;
    private readonly HashSet<Guid> cancelledTasks = new();
    private readonly List<ISubscription> subscriptions = new();

    private CancellationTokenSource tokenSource;
    private RoundRobinList<MessageQueue> roundRobin;
    private TaskCreatedMessage actualTask;
    private double timeout = 10.0;

    private void InitRoundRobin()
    {
        var exampleList = options.Queues.Select(i => new MessageQueue(busClient, i.Key)).ToList();
        roundRobin = new RoundRobinList<MessageQueue>(exampleList);
        foreach (var (Element, Priority) in exampleList.Zip(options.Queues.Values))
        {
            roundRobin.IncreaseWeight(Element, Priority);
        }
    }

    public LauncherWorker(ILogger<LauncherWorker> logger, 
        IOptions<QueuesPriorityConfiguration> options,
        IBusClient busClient, 
        IDefaultSubscriber subscriber, 
        IDefaultPublisher publisher, 
        IFileStorageService fileStorageService, 
        ITaskLauncherService launcher)
    {
        this.logger = logger;
        this.busClient = busClient;
        this.subscriber = subscriber;
        this.publisher = publisher;
        this.fileStorageService = fileStorageService;
        this.options = options.Value;
        InitRoundRobin();
        this.launcher = launcher;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        //task byl zrusen, odstran z pameti
        var cancelled = subscriber.SubscribeAsync<TaskCancelledMessage>((message, context) =>
        {
            if (cancelledTasks.Contains(message.TaskId))
                cancelledTasks.Remove(message.TaskId);
            return Task.CompletedTask;
        });

        //task se musi zrusit, pokud ho nevykonavam tak si radeji ulozim id tasku
        var cancel = subscriber.SubscribeAsync<CancelTaskMessage>((message, context) =>
        {
            if (actualTask.Id == message.TaskId)
                tokenSource.Cancel();
            else
                cancelledTasks.Add(message.TaskId);
            return Task.CompletedTask;
        });

        var configChanged = subscriber.SubscribeAsync<ConfigChangedMessage>((message, context) =>
        {
            if(message.Name == "tasktimeout")
            {
                double.TryParse(message.Value, out timeout);
            }
            return Task.CompletedTask;
        });

        subscriptions.Add(cancel);
        subscriptions.Add(cancelled);
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        tokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        while (true)
        {
            //prijmi task
            stoppingToken.ThrowIfCancellationRequested();
            var currentQueue = roundRobin.Next();
            var name = currentQueue.Queue;
            var mess = currentQueue.GetMessage<TaskCreatedMessage>();
            while (mess is null)
            {
                var qq = roundRobin.GetNextItem(currentQueue);
                mess = qq.GetMessage<TaskCreatedMessage>();
                if (name == qq.Queue)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    logger.LogInformation("No events, waiting");
                    await Task.Delay(5000, stoppingToken);
                }
                currentQueue = qq;
            }

            actualTask = mess.Message;

            //zkontroluj zda task neni zrusen
            if (cancelledTasks.Contains(actualTask.Id))
            {
                await publisher.PublishAsync(new TaskCancelledMessage { TaskId = mess.Message.Id });
                tokenSource.Cancel();
                mess.Ack();
                continue;
            }

            try
            {
                tokenSource.CancelAfter(TimeSpan.FromHours(timeout));
                await TaskExecution(mess.Message, tokenSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                if(actualTask.State != TaskState.Finished)
                {
                    await publisher.PublishAsync(new TaskCancelledMessage { TaskId = actualTask.Id });
                    logger.LogInformation("Task cancelled '{0}', ex: {1}", actualTask.Id, ex);
                }
                tokenSource.Dispose();
                tokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            }
            mess.Ack();
        }
    }

    private async Task TaskExecution(TaskCreatedMessage model, CancellationToken token)
    {
        logger.LogInformation("Starting execution of task '{0}'", model.Id);
        
        //stazeni souboru
        using (var file = File.Create("tmp/task.txt"))
        {
            await fileStorageService.DownloadFileAsync(model.TaskFilePath, file);
        }

        //poslani informace o pripraveni tasku k spusteni
        await UpdateTaskAsync(model, TaskState.Prepared);

        //kontrola zda neni task zrusen
        token.ThrowIfCancellationRequested();
        if (cancelledTasks.Contains(actualTask.Id))
        {
            tokenSource.Cancel();
        }

        //start kontejneru
        var tmp = await launcher.StartContainer(token);

        //poslani informace o behu kontejneru
        await UpdateTaskAsync(model, TaskState.Running);

        //cekani az se dokonci task
        await launcher.WaitContainer(tmp.ContainerId, token);

        //upload souboru s vysledkem
        using (var resultFile = File.Open("tmp/task.txt", FileMode.Open))
        {
            await fileStorageService.UploadFileAsync(model.ResultFilePath, resultFile);
        }
        await UpdateTaskAsync(model, TaskState.Finished);
        logger.LogInformation("Task '{0}' finished", model.Id);

        token.ThrowIfCancellationRequested();
    }

    private async Task UpdateTaskAsync(TaskCreatedMessage model, TaskState state)
    {
        model.State = state;
        model.Time = DateTime.Now;
        await publisher.PublishAsync(new UpdateTaskMessage { Id = model.Id, State = model.State, Time = model.Time });
    }
}
