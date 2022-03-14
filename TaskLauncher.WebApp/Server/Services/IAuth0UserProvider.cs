using Auth0.AuthenticationApi.Models;

namespace TaskLauncher.WebApp.Server.Services;

public interface IAuth0UserProvider
{
    Task<AccessTokenResponse> GetRefreshedAccessToken();
    Task<UserInfo> GetActualUser();
}
