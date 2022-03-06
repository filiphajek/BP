namespace TaskLauncher.Api.DAL.Entities;

public record BanEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public DateTime Started { get; set; }
    public DateTime? Ended { get; set; }
    public string Description { get; set; }
}
