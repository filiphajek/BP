using TaskLauncher.Common.Enums;

namespace TaskLauncher.App.DAL.Entities;

public record TaskEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public TaskState ActualStatus { get; set; } = TaskState.Created;
    public string TaskFile { get; set; } = string.Empty;
    public string ResultFile { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public ICollection<EventEntity> Events { get; set; }
    public bool IsPriority { get; set; }
}
