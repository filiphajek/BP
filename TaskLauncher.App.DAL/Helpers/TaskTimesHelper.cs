using TaskLauncher.App.DAL.Entities;
using TaskLauncher.Common.Enums;

namespace TaskLauncher.App.DAL.Helpers;

public static class TaskTimesHelper
{
    /// <summary>
    /// Pomocna funkce pro spocitani hodnoty udavajici kolik casu task stravil ve fronte a jak dlouho ho zpracovaval worker
    /// </summary>
    public static (TimeSpan TimeInQueue, TimeSpan CpuTime) GetTimeStats(TaskEntity task)
    {
        var timeInQueue = TimeSpan.Zero;
        var cpuTime = TimeSpan.Zero;

        var events = task.Events.OrderBy(i => i.Time);
        var tmpTime = TimeSpan.Zero;
        var lastTime = DateTime.MinValue;
        foreach (var ev in events)
        {
            if (ev.Status == TaskState.Created)
            {
                lastTime = ev.Time;
                tmpTime = TimeSpan.Zero;
                continue;
            }

            tmpTime += ev.Time - lastTime;
            lastTime = ev.Time;
            if (ev.Status == TaskState.Ready)
            {
                timeInQueue += tmpTime;
                tmpTime = TimeSpan.Zero;
            }
            if (ev.Status == TaskState.FinishedSuccess || ev.Status == TaskState.FinishedFailure || ev.Status == TaskState.Crashed || ev.Status == TaskState.Timeouted)
            {
                cpuTime += tmpTime;
                tmpTime = TimeSpan.Zero;
            }
        }
        return new(timeInQueue, cpuTime);
    }
}