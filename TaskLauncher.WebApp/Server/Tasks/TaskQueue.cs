using TaskLauncher.Common.Models;

namespace TaskLauncher.WebApp.Server.Tasks;

public class TaskQueue : PriorityQueue<TaskModel, DateTime>
{
    public string Name { get; init; }

    public TaskQueue(string name)
    {
        Name = name;
    }
}
