﻿using Microsoft.AspNetCore.SignalR;
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
        //taskQueues = options.Value.Queues.ToDictionary(i => i.Key, i => new TaskQueue(i.Key));
        //var newWay = new RoundRobinList<TaskQueue>(taskQueues.Values, options.Value.Queues.Select(i => i.Value).ToArray());

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

    public void CancelTask(Guid id)
    {
        canceledTasks.Add(id);
    }

    private bool TryDequeue(TaskQueue queue, out TaskModel? task)
    {
        if (queue.TryDequeue(out task, out _))
        {
            if (canceledTasks.Contains(task.Id))
            {
                task = null;
                return false;
            }
            return true;
        }
        return false;
    }

    public void Enqueue(string queue, TaskModel task)
    {
        if (taskQueues.TryGetValue(queue, out var taskQueue))
        {
            taskQueue.Enqueue(task, task.Time);
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