namespace TaskLauncher.Api.Contracts.Requests;

public record TaskUpdateRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
