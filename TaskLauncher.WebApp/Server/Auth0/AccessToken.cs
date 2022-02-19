namespace TaskLauncher.WebApp.Server.Auth0;

public class AccessToken
{
    public string AcessToken { get; init; } = string.Empty;
    public DateTime ExpiresIn { get; init; }
}
