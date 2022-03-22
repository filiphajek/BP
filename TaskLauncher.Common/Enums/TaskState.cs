namespace TaskLauncher.Common.Enums;

public enum TaskState
{
    Created,
    Ready,
    Running,
    Cancelled,
    Crashed,
    Timeouted,
    FinishedSuccess,
    FinishedFailure,
    Downloaded,
    Closed,
}
