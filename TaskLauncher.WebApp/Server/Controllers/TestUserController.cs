using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.WebApp.Server.Auth0;

namespace TaskLauncher.WebApp.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class TestUserController : ControllerBase
{
    private readonly ManagementTokenService managementTokenService;

    public TestUserController(ManagementTokenService managementTokenService)
    {
        this.managementTokenService = managementTokenService;
    }

    private class AccessTokenItem
    {
        public string access_token { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> GetUserInfo()
    {
        var accessToken = await managementTokenService.GetApiToken(new(), "managment_api");

        var auth0client = new ManagementApiClient(accessToken, new Uri("https://dev-8nhuxay1.us.auth0.com/api/v2"));
        var list = await auth0client.Users.GetAllAsync(new GetUsersRequest { });
        var tmp = await auth0client.Users.GetAsync("auth0|61b0e161678a0c00689644e0");
        return Ok(tmp);
    }
}
