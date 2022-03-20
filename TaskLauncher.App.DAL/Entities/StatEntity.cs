namespace TaskLauncher.App.DAL.Entities;

public record StatEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public int AllTaskCount { get; set; }
    public int FinishedTaskCount { get; set; }
    public int FailedTasks { get; set; }
    public int SuccessTasks { get; set; }
    public int TimeoutedTasks { get; set; }
    public int CrashedTasks { get; set; }
}
