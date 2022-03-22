using TaskLauncher.Common.Enums;

namespace TaskLauncher.Common.Extensions;

public static class TaskStateExtensions
{
    public static bool TaskFinished(this TaskState state)
    {
        if(state == TaskState.FinishedSuccess || state == TaskState.FinishedFailure || state == TaskState.Crashed || state == TaskState.Timeouted)
            return true;
        return false;
    }
}