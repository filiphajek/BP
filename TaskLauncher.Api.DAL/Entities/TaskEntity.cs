using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.DAL.Entities;

public record TaskEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public TaskState Status { get; set; } = TaskState.Created;
    public ICollection<FileEntity> Files { get; set; }
}