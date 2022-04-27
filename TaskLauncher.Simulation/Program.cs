using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskLauncher.App.DAL.Installers;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Simulation;

await Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        //databaze pro stazeni vysledku
        new DatabaseInstaller().Install(services, context.Configuration);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        //konfigurace
        services.Configure<SimulationConfig>(context.Configuration.GetSection(nameof(SimulationConfig)));
        services.Configure<Auth0ApiConfiguration>(context.Configuration.GetSection(nameof(Auth0ApiConfiguration)));
        services.Configure<ServiceAddresses>(context.Configuration.GetSection(nameof(ServiceAddresses)));
        ServiceAddresses config = new();
        context.Configuration.Bind(nameof(ServiceAddresses), config);

        //http klient
        services.AddHttpClient("default", client => client.BaseAddress = config.WebApiAddressUri)
            .ConfigurePrimaryHttpMessageHandler(builder =>
                new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }
            );
        services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("default"));

        //services
        services.AddSingleton<UserFactory>();
        services.AddHostedService<SimulationService>();
    })
    .Build()
    .RunAsync();
