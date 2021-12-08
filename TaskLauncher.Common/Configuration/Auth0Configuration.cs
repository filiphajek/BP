namespace TaskLauncher.Common.Configuration;

public record Auth0Configuration
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Audience { get; set; }
    public string Domain { get; set; }
}
