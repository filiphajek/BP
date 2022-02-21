namespace TaskLauncher.Api.DAL.Entities;

public record TokenBalanceEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public double CurrentAmount { get; set; }
    public DateTime LastAdded { get; set; }
}
