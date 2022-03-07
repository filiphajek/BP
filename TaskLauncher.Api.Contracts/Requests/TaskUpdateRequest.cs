namespace TaskLauncher.Api.Contracts.Requests;

public record TaskUpdateRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public record BanUserRequest
{
    public string UserId { get; set; }
    public string Reason { get; set; }
}