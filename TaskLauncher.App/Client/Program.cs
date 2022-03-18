using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaskLauncher.App.Client;
using TaskLauncher.Common.Configuration;
using MapsterMapper;
using Mapster;
using Blazored.Toast;
using Radzen;
using TaskLauncher.Authorization;
using TaskLauncher.App.Client.Services;
using TaskLauncher.App.Client.Authentication;
using Blazored.LocalStorage;
using TaskLauncher.App.Client.Store;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//konfigurace
var serviceAddresses = new ServiceAddresses();
builder.Configuration.Bind(nameof(ServiceAddresses), serviceAddresses);
builder.Services.AddSingleton(serviceAddresses);
var auth0Config = new Auth0ApiClientConfiguration();
builder.Configuration.Bind(nameof(Auth0ApiClientConfiguration), auth0Config);
builder.Services.AddSingleton(auth0Config);

//mapper
var config = new TypeAdapterConfig();
config.Scan(typeof(Program).Assembly);
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();

//notifikace
builder.Services.AddBlazoredToast();
builder.Services.AddSingleton<SignalRClient>();

//dialogy
builder.Services.AddScoped<DialogService>();

//auth0 klient
builder.Services.AddScoped<SpaManagementApiClient>();

//user provider
builder.Services.AddScoped<IUserProvider, UserProvider>();

//autorizace
builder.Services.AddOptions();
builder.Services.AddAuthorizationClient();
builder.Services.AddSingleton<AuthenticationStateProvider, HostAuthenticationStateProvider>();
builder.Services.AddSingleton(sp => (HostAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

//http klient
builder.Services.AddTransient<BanHandler>();
builder.Services.AddHttpClient("default", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BanHandler>();

builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("default"));

//local storage
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<TokenStore>();

await builder.Build().RunAsync();