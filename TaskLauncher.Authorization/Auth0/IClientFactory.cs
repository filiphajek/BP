namespace TaskLauncher.Authorization.Auth0;

/// <summary>
/// Interface pro klientske tovarny
/// </summary>
public interface IClientFactory<TClient>
    where TClient : class
{
    public abstract Task<TClient> GetClient();
}
