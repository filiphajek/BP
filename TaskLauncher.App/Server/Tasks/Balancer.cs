using MediatR;
using Microsoft.Extensions.Options;
using RoundRobin;
using TaskLauncher.App.Server.Notifications;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Tasks;

/// <summary>
/// Trida se stara o vsechny fronty
/// Zajistuje aby neprioritni fronty nehladovaly algoritmem round robin
/// </summary>
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

    /// <summary>
    /// Rusi task, dochazi k zamceni front, aby nedochazelo k situacim, kdy se zrusi task, ale kvuli zmene kontextu se stihne task poslat na worker
    /// </summary>
    public bool CancelTask(Guid id)
    {
        canceledTasks.Add(id);
        semaphore.Wait();
        bool result = false;
        foreach (var queue in taskQueues.Values)
        {
            if (queue.Any(i => i.Id == id))
            {
                logger.LogInformation("Task {0} cancelled", id);
                canceledTasks.Add(id);
                result = true;
            }
        }
        semaphore.Release();
        return result;
    }

    /// <summary>
    /// Pomocna metoda pro odebrani prvku z fronty, dochazi k zamykani fronty, kvuli kontrole ruseni tasku
    /// </summary>
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

    /// <summary>
    /// Kontroluje zda je task v nektere z front
    /// </summary>
    public bool Exists(TaskModel task)
    {
        foreach (var queue in taskQueues)
        {
            if (queue.Value.Any(i => i.Id == task.Id))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Vlozi novy task do patricne fronty
    /// </summary>
    public void Enqueue(string queue, TaskModel task)
    {
        if (taskQueues.TryGetValue(queue, out var taskQueue))
        {
            logger.LogInformation("Task {0} enqueued", task.Id);
            taskQueue.Enqueue(task);
            if(taskQueues.Select(i => i.Value).Sum(i => i.Count) <= 1)
                mediator.Publish(new NewTaskNotification(task)).Wait();
        }
    }

    /// <summary>
    /// Podle algoritmu round robin, ziska dalsi task z front
    /// </summary>
    public TaskModel? GetNext()
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
                return null;
            
            current = next;
        }
    }
}