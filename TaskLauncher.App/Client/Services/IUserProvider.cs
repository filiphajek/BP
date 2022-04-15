using Auth0.ManagementApi.Models;
using GridShared;
using GridShared.Utility;
using Microsoft.Extensions.Primitives;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Client.Services;

/// <summary>
/// Definice pro UserProvider poskytujici seznam uzivatelu pro grid
/// </summary>
public interface IUserProvider
{
    Task<ItemsDTO<UserModel>> GetUsers(Action<IGridColumnCollection<UserModel>> columns, QueryDictionary<StringValues> query);
}
