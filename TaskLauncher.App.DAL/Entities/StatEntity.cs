namespace TaskLauncher.App.DAL.Entities;

public record StatEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public int AllTaskCount { get; set; } = 0;
    public int FinishedTaskCount { get; set; } = 0;
    public int FailedTasks { get; set; } = 0;
    public int SuccessTasks { get; set; } = 0;
    public int TimeoutedTasks { get; set; } = 0;
    public int CrashedTasks { get; set; } = 0;
    public bool IsVip { get; set; }
}
