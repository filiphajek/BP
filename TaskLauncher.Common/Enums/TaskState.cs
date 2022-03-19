namespace TaskLauncher.Common.Enums;

public enum TaskState
{
    Created,
    Ready,
    Running,
    Cancelled,
    Crashed,
    FinishedSuccess,
    FinishedFailure,
    Downloaded,
    Closed,
}
