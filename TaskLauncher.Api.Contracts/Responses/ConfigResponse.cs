namespace TaskLauncher.Api.Contracts.Responses;

public record ConfigResponse
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string Description { get; set; }
    public bool CanDelete { get; set; } = true;
}
