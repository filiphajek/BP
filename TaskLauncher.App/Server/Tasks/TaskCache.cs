using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Tasks;

/// <summary>
/// Cache tasku pro signalr
/// </summary>
public class TaskCache : Dictionary<string, TaskModel> { }
