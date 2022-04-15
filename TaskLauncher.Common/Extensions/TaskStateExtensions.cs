using TaskLauncher.Common.Enums;

namespace TaskLauncher.Common.Extensions;

/// <summary>
/// Extenze k TaskState enum
/// </summary>
public static class TaskStateExtensions
{
    /// <summary>
    /// Urcuje zda je task v jednom z ukoncenych stavu
    /// </summary>
    public static bool TaskFinished(this TaskState state)
    {
        if(state == TaskState.FinishedSuccess || state == TaskState.FinishedFailure || state == TaskState.Crashed || state == TaskState.Timeouted)
            return true;
        return false;
    }
}