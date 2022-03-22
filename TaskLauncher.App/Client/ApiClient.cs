using TaskLauncher.App.Client.Authentication;

namespace TaskLauncher.App.Client;

public class ApiClient : HttpClient
{
    public HttpClient Client { get; }
    
    public ApiClient(HttpClient client, BanHandler banHandler) : base(banHandler)
    {
        banHandler.InnerHandler = new HttpClientHandler();
        Client = client;
        BaseAddress = client.BaseAddress;
    }
}
