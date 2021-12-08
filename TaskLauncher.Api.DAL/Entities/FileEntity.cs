namespace TaskLauncher.Api.DAL.Entities;

public record FileEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public TaskEntity Task { get; set; }
}
