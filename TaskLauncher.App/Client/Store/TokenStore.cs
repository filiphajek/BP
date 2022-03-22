using Blazored.LocalStorage;

namespace TaskLauncher.App.Client.Store;

public class TokenStore
{
    private readonly ILocalStorageService storage;

    public string Value { get; } = "";

    public event Func<Task> OnBalanceChange;

    public TokenStore(ILocalStorageService storage)
    {
        this.storage = storage;
    }

    public async Task<string> GetBalanceAsync()
    {
        return await storage.GetItemAsStringAsync("token");
    }

    public async Task UpdateBalanceAsync(string balance)
    {
        await storage.SetItemAsStringAsync("token", balance);
        
        if(OnBalanceChange is not null)
            await OnBalanceChange.Invoke();
    }
}
