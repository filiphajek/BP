namespace TaskLauncher.Api.Contracts.Responses;

public record TaskStatResponse
{
    public TaskStatResponse(bool isVip, string taskName, TimeSpan timeInQueue, TimeSpan cpuTime)
    {
        IsVip = isVip;
        TaskName = taskName;
        TimeInQueue = timeInQueue;
        CpuTime = cpuTime;
    }

    /// <summary>
    /// Určuje zda byla úloha prioritní
    /// </summary>
    /// <example>true</example>
    public bool IsVip { get; }

    /// <summary>
    /// Jméno úlohy
    /// </summary>
    /// <example>Úloha1</example>
    public string TaskName { get; }

    /// <summary>
    /// Čas strávený ve frontě
    /// </summary>
    public TimeSpan TimeInQueue { get; }

    /// <summary>
    /// Čas strávený v kontejneru
    /// </summary>
    public TimeSpan CpuTime { get; }
}
