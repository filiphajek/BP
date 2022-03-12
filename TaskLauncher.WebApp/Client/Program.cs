using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaskLauncher.WebApp.Client;
using TaskLauncher.WebApp.Client.Authentication;
using System.Net.Http.Headers;
using TaskLauncher.Common.Configuration;
using TaskLauncher.WebApp.Client.Services;
using MapsterMapper;
using Mapster;
using Blazored.Toast;
using Radzen;
using TaskLauncher.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//konfigurace adres, zde pouze pro signalr endpoint
var serviceAddresses = new ServiceAddresses();
builder.Configuration.Bind(nameof(ServiceAddresses), serviceAddresses);
builder.Services.AddSingleton(serviceAddresses);

builder.Services.AddScoped<DialogService>();

//notifikace
builder.Services.AddBlazoredToast();
builder.Services.AddSingleton<SignalRClient>();

//registrace httpclienta
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
//registrace pojmenovaneho httpclienta
builder.Services.AddHttpClient("default", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("default"));

Auth0ApiClientConfiguration auth0Config = new();
builder.Configuration.Bind(nameof(Auth0ApiClientConfiguration), auth0Config);
builder.Services.AddSingleton(auth0Config);
builder.Services.AddScoped<SpaManagementApiClient>();

builder.Services.AddScoped<IUserProvider, UserProvider>();

var config = new TypeAdapterConfig();
config.Scan(typeof(Program).Assembly);
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();

//autorizace
builder.Services.AddAuthorizationClient();
builder.Services.AddSingleton<AuthenticationStateProvider, HostAuthenticationStateProvider>();
builder.Services.AddSingleton(sp => (HostAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());
builder.Services.AddTransient<AuthorizedHandler>();

await builder.Build().RunAsync();
