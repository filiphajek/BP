namespace TaskLauncher.Api.Contracts.Requests;

public record TaskCreateRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
