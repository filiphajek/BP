namespace TaskLauncher.Authorization.Auth0;

/// <summary>
/// DTO model pristupoveho tokenu
/// </summary>
public class AccessToken
{
    public string AcessToken { get; init; } = string.Empty;
    public DateTime ExpiresIn { get; init; }
}
