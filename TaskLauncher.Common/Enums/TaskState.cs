namespace TaskLauncher.Common.Enums;

public enum TaskState
{
    Created,
    Ready,
    Running,
    FinishedSuccess,
    FinishedFailure,
    Closed,
    Cancelled,
    Crashed,
    Downloaded
}
