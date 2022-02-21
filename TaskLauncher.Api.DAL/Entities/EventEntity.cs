using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.DAL.Entities;

public record EventEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public TaskState Status { get; set; }
    public DateTime Time { get; set; }
    public TaskEntity Task { get; set; }
}
