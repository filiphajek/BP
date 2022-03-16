namespace TaskLauncher.App.DAL.Entities;

public record BaseEntity : IEntity
{
    public Guid Id { get; init; }
}
