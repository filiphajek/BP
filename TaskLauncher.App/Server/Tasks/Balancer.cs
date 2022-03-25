using MediatR;
using Microsoft.Extensions.Options;
using RoundRobin;
using TaskLauncher.App.Server.Notifications;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Tasks;

public class Balancer
{
    private readonly RoundRobinList<TaskQueue> roundRobin;
    private readonly Dictionary<string, TaskQueue> taskQueues;
    private readonly HashSet<Guid> canceledTasks = new();
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private readonly ILogger<Balancer> logger;
    private readonly IMediator mediator;
    
    public Balancer(ILogger<Balancer> logger, IOptions<PriorityQueuesConfiguration> options, IMediator mediator)
    {
        taskQueues = options.Value.Queues.ToDictionary(i => i.Key, i => new TaskQueue(i.Key));
        roundRobin = new RoundRobinList<TaskQueue>(taskQueues.Values, options.Value.Queues.Select(i => i.Value).ToArray());
        this.logger = logger;
        this.mediator = mediator;
    }

    public bool CancelTask(Guid id)
    {
        canceledTasks.Add(id);
        semaphore.Wait();
        bool result = false;
        foreach (var queue in taskQueues.Values)
        {
            if (queue.Any(i => i.Id == id))
            {
                canceledTasks.Add(id);
                result = true;
            }
        }
        semaphore.Release();
        return result;
    }

    private bool TryDequeue(TaskQueue queue, out TaskModel? task)
    {
        semaphore.Wait();
        while (queue.TryDequeue(out task))
        {
            if (canceledTasks.Contains(task.Id))
                continue;
            semaphore.Release();
            return true;
        }
        semaphore.Release();
        return false;
    }

    public void Enqueue(string queue, TaskModel task)
    {
        if (taskQueues.TryGetValue(queue, out var taskQueue))
        {
            taskQueue.Enqueue(task);
            if(taskQueues.Select(i => i.Value).Sum(i => i.Count) <= 1)
                mediator.Publish(new NewTaskNotification(task)).Wait();
        }
    }

    public async Task<TaskModel> GetNext(CancellationToken token = default)
    {
        var current = roundRobin.Next();
        if (TryDequeue(current, out var task))
            return task!;

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