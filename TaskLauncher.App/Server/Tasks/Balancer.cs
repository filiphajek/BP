using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RoundRobin;
using TaskLauncher.App.Server.Hub;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Tasks;

public class Balancer
{
    private readonly RoundRobinList<TaskQueue> roundRobin;
    private readonly Dictionary<string, TaskQueue> taskQueues;
    private readonly ILogger<Balancer> logger;
    private readonly IHubContext<WorkerHub, IWorkerHub> workerHub;
    private readonly HashSet<Guid> canceledTasks = new();

    public bool ClientsWithoutWork { get; set; }

    public Balancer(ILogger<Balancer> logger, IOptions<PriorityQueuesConfiguration> options, IHubContext<WorkerHub, IWorkerHub> workerHub)
    {
        taskQueues = options.Value.Queues.ToDictionary(i => i.Key, i => new TaskQueue(i.Key));
        roundRobin = new RoundRobinList<TaskQueue>(taskQueues.Values, options.Value.Queues.Select(i => i.Value).ToArray());
        this.logger = logger;
        this.workerHub = workerHub;
    }

    private readonly SemaphoreSlim semaphore = new(1, 1);

    public bool CancelTask(Guid id)
    {
        //TODO semafor tu a pak v TryDequeue metode -> budu moct garantovat ze opravdu vyradim task z fronty
        canceledTasks.Add(id);
        return true;
        /*foreach (var queue in taskQueues.Values)
        {
            if (queue.Contains(model))
            {
                canceledTasks.Add(model.Id);
                return;
            }
        }*/
    }

    private bool TryDequeue(TaskQueue queue, out TaskModel? task)
    {
        while (queue.TryDequeue(out task))
        {
            if (canceledTasks.Contains(task.Id))
                continue;
            return true;
        }
        return false;
    }

    public void Enqueue(string queue, TaskModel task)
    {
        if (taskQueues.TryGetValue(queue, out var taskQueue))
        {
            taskQueue.Enqueue(task);
            if (ClientsWithoutWork)
            {
                workerHub.Clients.All.IsWorking().Wait();
                ClientsWithoutWork = false;
            }
        }
    }

    public async Task<TaskModel> GetNext(CancellationToken token = default)
    {
        var current = roundRobin.Next();
        if (TryDequeue(current, out var task))
            return task!;

        //nahradit timerem?
        var name = current.Name;
        while (true)
        {
            var next = roundRobin.GetNextItem(current);
            if (TryDequeue(next, out var task2))
                return task2!;
            if (name == next.Name)
            {
                logger.LogInformation("Waiting for new tasks");
                await Task.Delay(3000, token);
            }
            current = next;
        }
    }
}