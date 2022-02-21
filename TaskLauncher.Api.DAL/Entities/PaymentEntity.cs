namespace TaskLauncher.Api.DAL.Entities;

public record PaymentEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public TaskEntity Task { get; set; }
    public DateTime Time { get; set; }
    public double Price { get; set; }
}
