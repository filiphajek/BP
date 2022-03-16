using Auth0.ManagementApi.Models;
using GridShared;
using GridShared.Utility;
using Microsoft.Extensions.Primitives;

namespace TaskLauncher.App.Client.Services;

public interface IUserProvider
{
    Task<ItemsDTO<User>> GetUsers(Action<IGridColumnCollection<User>> columns, QueryDictionary<StringValues> query);
}
