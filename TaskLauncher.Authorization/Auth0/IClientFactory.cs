namespace TaskLauncher.Authorization.Auth0;

public interface IClientFactory<TClient>
    where TClient : class
{
    public abstract Task<TClient> GetClient();
}
