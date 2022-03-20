namespace TaskLauncher.Api.Contracts.Responses;

public record UserStatResponse
{
    public int AllTaskCount { get; set; }
    public int FinishedTaskCount { get; set; }
    public int FailedTasks { get; set; }
    public int SuccessTasks { get; set; }
    public int TimeoutedTasks { get; set; }
    public int CrashedTasks { get; set; }
    public bool IsVip { get; set; }
}

public record TaskStatResponse(bool IsVip, string TaskName, TimeSpan TimeInQueue, TimeSpan CpuTime);

public record DayTaskCountResponse
{
    public DayTaskCountResponse(int count, DateTime date)
    {
        Count = count;
        Date = date;
    }

    public int Count { get; set; }
    public DateTime Date { get; set; }
}
