using Auth0.ManagementApi.Models;
using GridCore.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.Extensions.Primitives;

namespace TaskLauncher.App.Client.Services;

public class UserProvider : IUserProvider
{
    private readonly SpaManagementApiClient auth0client;

    public UserProvider(SpaManagementApiClient auth0client)
    {
        this.auth0client = auth0client;
    }

    public async Task<ItemsDTO<User>> GetUsers(Action<IGridColumnCollection<User>> columns, QueryDictionary<StringValues> query)
    {
        var users = (await auth0client.Users.GetAllAsync(new())).ToList();

        var server = new GridCoreServer<User>(users, query, true, "usersGrid", columns).Sortable();
        var items = server.ItemsToDisplay;
        return items;
    }
}
