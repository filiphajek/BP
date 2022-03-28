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
