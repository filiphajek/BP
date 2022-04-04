namespace TaskLauncher.Api.Contracts.Requests;

public record UpdateProfileRequest
{
    public string Picture { get; set; }
    public string Nickname { get; set; }
}