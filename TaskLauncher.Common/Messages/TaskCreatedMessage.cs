using TaskLauncher.Common.Enums;

namespace TaskLauncher.Common.Messages;

public class TaskCreatedMessage
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public TaskState State { get; set; } = TaskState.Created;
    public DateTime Time { get; set; }
    public string TaskFilePath { get; set; }
    public string ResultFilePath { get; set; }
}

public class TaskCancelledMessage
{
    public Guid TaskId { get; set; }
}

public class CancelTaskMessage
{
    public Guid TaskId { get; set; }
}

public class UpdateTaskMessage
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public TaskState State { get; set; }
}

public class ConfigChangedMessage
{
    public string Name { get; set; }
    public string Value { get; set; }
}
