namespace TaskLauncher.Api.Contracts.Requests;

public record TaskUpdateRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
