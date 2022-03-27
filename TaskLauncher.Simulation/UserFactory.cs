using Auth0.ManagementApi;
using Bogus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Json;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.Simulation;

public class UserFactory
{
    private readonly HttpClient client = new();
    private readonly Auth0ApiConfiguration config;
    private readonly ServiceAddresses options;
    private ManagementApiClient auth0client;

    public UserFactory(IOptions<Auth0ApiConfiguration> config, IOptions<ServiceAddresses> options)
    {
        this.config = config.Value;
        this.options = options.Value;
    }

    public async Task Initialize()
    {
        var token = await GetTokenAsync();
        auth0client = new ManagementApiClient(token, options.WebApiAddressUri.Authority);
    }

    public async Task RemoveUser(string userId)
    {
        await auth0client.Users.DeleteAsync(userId);
    }

    public async Task<UserModel> CreateUser(bool vip)
    {
        var faker = new Faker<UserModel>()
            .RuleFor(i => i.FirstName, x => x.Name.FirstName())
            .RuleFor(i => i.FirstName, x => x.Name.LastName())
            .RuleFor(i => i.NickName, (x, y) => y.FirstName)
            .RuleFor(i => i.Email, (x, y) => x.Internet.Email(y.FirstName, y.LastName));

        var name = faker.Generate();
        var registration = await client.PostAsJsonAsync("registerbypasswordflow", new CookieLessUserRegistrationModel
        {
            FirstName = name.FirstName,
            LastName = name.LastName,
            Email = name.Email,
            NickName = name.NickName,
            Password = "Password123*",
            PhoneNumber = "+420123456789"
        });

        var user = await registration.Content.ReadFromJsonAsync<UserModel>();
        var resultUser = await auth0client.Users.UpdateAsync(user!.UserId, new() { AppMetadata = JsonConvert.DeserializeObject($"{{ 'vip': {vip.ToString().ToLower()} }}") });
        return resultUser.GetModel();
    }

    private record AccessTokenItem(string access_token);
    
    private async Task<string> GetTokenAsync()
    {
        var payload = new
        {
            client_id = config.ClientId,
            client_secret = config.ClientSecret,
            audience = config.Audience,
            grant_type = "client_credentials"
        };

        var tokenResponse = await client.PostAsJsonAsync($"https://{config.Domain}/oauth/token/", payload);

        if (tokenResponse.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var result = await tokenResponse.Content.ReadFromJsonAsync<AccessTokenItem>();
            return result!.access_token;
        }
        throw new ApplicationException("Access token was not generated");
    }
}
