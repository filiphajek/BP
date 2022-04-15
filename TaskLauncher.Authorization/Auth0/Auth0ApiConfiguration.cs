namespace TaskLauncher.Authorization.Auth0;

/// <summary>
/// Konfigurace auth0 sluzby
/// </summary>
public class Auth0ApiConfiguration
{
    public string Audience { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Domain { get; set; }
}
