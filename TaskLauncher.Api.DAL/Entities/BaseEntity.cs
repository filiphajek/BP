namespace TaskLauncher.Api.DAL.Entities;

public record BaseEntity : IEntity
{
    public Guid Id { get; init; }
}
