using Auth0.ManagementApi.Paging;
using GridCore.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.Extensions.Primitives;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Client.Services;

/// <summary>
/// Trida implementujici IUserProvider poskytujici pres auth0 seznam uzivatelu pro grid
/// </summary>
public class UserProvider : IUserProvider
{
    private readonly SpaManagementApiClient auth0client;

    public UserProvider(SpaManagementApiClient auth0client)
    {
        this.auth0client = auth0client;
    }

    private readonly List<UserModel> cachedUsers = new();

    public async Task<ItemsDTO<UserModel>> GetUsers(Action<IGridColumnCollection<UserModel>> columns, QueryDictionary<StringValues> query)
    {
        PaginationInfo pagination = new();

        if(query.Count >= 3)
            return new GridCoreServer<UserModel>(cachedUsers, query, true, "usersGrid", columns, 10).Sortable().ItemsToDisplay;

        if (query.TryGetValue("grid-page", out var page) && query.TryGetValue("grid-pagesize", out var size))
            pagination = new PaginationInfo(int.Parse(page), int.Parse(size));

        var users = (await auth0client.Users.GetAllAsync(new() { }, pagination)).Select(i => i.GetModel()).Where(i => !i.IsAdmin);
        cachedUsers.Clear();
        cachedUsers.AddRange(users);

        var server = new GridCoreServer<UserModel>(users, query, true, "usersGrid", columns, 10).Sortable();
        return server.ItemsToDisplay;
    }
}
