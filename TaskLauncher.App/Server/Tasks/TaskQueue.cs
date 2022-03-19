using System.Collections.Concurrent;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Tasks;

public class TaskQueue : ConcurrentQueue<TaskModel>
{
    public string Name { get; init; }

    public TaskQueue(string name)
    {
        Name = name;
    }
}
