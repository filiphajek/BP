using TaskLauncher.Common.Enums;

namespace TaskLauncher.Common.Messages;

public class TaskCreated
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public TaskState State { get; set; } = TaskState.Created;
    public DateTime Time { get; set; }
    public string TaskFilePath { get; set; }
    public string ResultFilePath { get; set; }
}

public class TaskCancelled
{
    public Guid TaskId { get; set; }
}

public class CancelTask
{
    public Guid TaskId { get; set; }
}

public class UpdateTask
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public TaskState State { get; set; }
}

public class ConfigChanged
{
    public string Name { get; set; }
    public string Value { get; set; }
}
