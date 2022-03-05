using Auth0.ManagementApi.Models;
using GridCore.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.Extensions.Primitives;

namespace TaskLauncher.WebApp.Client.Services;

public class UserProvider
{
    public static async Task<ItemsDTO<User>> GetUsers(Action<IGridColumnCollection<User>> columns, QueryDictionary<StringValues> query)
    {
        SpaManagementApiClient apiClient = new("localhost:5001/auth0api");
        var users = (await apiClient.Users.GetAllAsync(new())).ToList();
        
        var server = new GridCoreServer<User>(users, query, true, "usersGrid", columns).Sortable();
        var items = server.ItemsToDisplay;
        return items;
    }
}
