using Auth0.AuthenticationApi.Models;

namespace TaskLauncher.Authorization.Services;

public interface IAuth0UserProvider
{
    Task<UserInfo> GetActualUser();
}
