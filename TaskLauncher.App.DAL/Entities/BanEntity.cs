namespace TaskLauncher.App.DAL.Entities;

public record BanEntity : BaseEntity, IUserKeyProtection
{
    public string Email { get; set; }
    public string UserId { get; set; }
    public DateTime Started { get; set; }
    public DateTime? Ended { get; set; }
    public string Description { get; set; }
}
