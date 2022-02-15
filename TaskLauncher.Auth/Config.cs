using IdentityServer4.Models;

namespace TaskLauncher.Auth;

public class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new[]
        {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResource
                {
                    Name = "role",
                    UserClaims = new List<string> { "role" }
                }
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new[] { new ApiScope("Task.read"), new ApiScope("Task.write"), };

    public static IEnumerable<ApiResource> ApiResources =>
        new[]
        {
                new ApiResource("TaskLauncherAPI")
                {
                    Scopes = new List<string> { "Task.read", "Task.write" },
                    ApiSecrets = new List<Secret> { new Secret("ScopeSecret".Sha256()) },
                    UserClaims = new List<string> { "role" }
                }
        };

    public static IEnumerable<Client> Clients =>
        new[]
        {
                // m2m client credentials flow client
                new Client
                {
                    ClientId = "m2m.client",
                    ClientName = "Client Credentials Client",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets = { new Secret("ClientSecret1".Sha256()) },
                    AllowedScopes = { "CoffeeAPI.read", "CoffeeAPI.write" }
                },
                // interactive client using code flow + pkce
                new Client
                {
                    ClientId = "gateway",
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    //AllowedGrantTypes = GrantTypes.Hybrid,
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = { "https://localhost:5001/signin-oidc" },
                    FrontChannelLogoutUri = "https://localhost:5001/signout-oidc",
                    PostLogoutRedirectUris = { "https://localhost:5001/signout-callback-oidc" },
                    AllowOfflineAccess = true,
                    AllowedScopes = { "openid", "profile"/*, "email"*/ },
                    RequirePkce = true,
                    RequireConsent = true,
                    AllowPlainTextPkce = false
                },
        };
}