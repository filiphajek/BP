using Microsoft.AspNetCore.Components;

namespace TaskLauncher.App.Client.Pages;

public abstract class BasePage : ComponentBase
{
    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }
}
