using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RoundRobin;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;
using TaskLauncher.WebApp.Server.Hub;

namespace TaskLauncher.WebApp.Server.Tasks;

public class Balancer
{
    private readonly RoundRobinList<TaskQueue> roundRobin;
    private readonly Dictionary<string, TaskQueue> taskQueues;
    private readonly ILogger<Balancer> logger;
    private readonly IHubContext<WorkerHub, IWorkerHub> workerHub;

    public bool ClientsWithoutWork { get; set; }

    public Balancer(ILogger<Balancer> logger, IOptions<PriorityQueuesConfiguration> options, IHubContext<WorkerHub, IWorkerHub> workerHub)
    {
        taskQueues = options.Value.Queues.ToDictionary(i => i.Key, i => new TaskQueue(i.Key));
        var cancelQueue = new TaskQueue("cancel");
        taskQueues.Add(cancelQueue.Name, cancelQueue);
        roundRobin = new RoundRobinList<TaskQueue>(taskQueues.Values);
        foreach (var (Queue, Priority) in taskQueues.Values.Zip(options.Value.Queues.Values))
        {
            roundRobin.IncreaseWeight(Queue, Priority);
        }
        roundRobin.IncreaseWeight(cancelQueue, 10);
        this.logger = logger;
        this.workerHub = workerHub;
    }

    public void Enqueue(string queue, TaskModel task)
    {
        if (taskQueues.TryGetValue(queue, out var taskQueue))
        {
            taskQueue.Enqueue(task, task.Time);
            if(ClientsWithoutWork)
            {
                workerHub.Clients.All.IsWorking().Wait();
                ClientsWithoutWork = false;
            }
        }
    }

    public async Task<TaskModel> GetNext(CancellationToken token = default)
    {
        var current = roundRobin.Next();
        if (current.TryDequeue(out var task, out _))
            return task;

        //nahradit timerem?
        var name = current.Name;
        while (true)
        {
            var next = roundRobin.GetNextItem(current);
            if (next.TryDequeue(out var task2, out _))
                return task2;
            if (name == next.Name)
            {
                logger.LogInformation("Waiting for new tasks");
                await Task.Delay(3000, token);
            }
            current = next;
        }
    }
}