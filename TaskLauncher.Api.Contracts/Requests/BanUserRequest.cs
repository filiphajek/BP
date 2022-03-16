namespace TaskLauncher.Api.Contracts.Requests;

public record BanUserRequest
{
    public string UserId { get; set; }
    public string Reason { get; set; }
}
