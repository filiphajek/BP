using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Simulation;

await Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.Configure<SimulationConfig>(context.Configuration.GetSection(nameof(SimulationConfig)));
        services.Configure<Auth0ApiConfiguration>(context.Configuration.GetSection(nameof(Auth0ApiConfiguration)));
        services.Configure<ServiceAddresses>(context.Configuration.GetSection(nameof(ServiceAddresses)));

        services.AddSingleton<UserFactory>();
        services.AddHostedService<SimulationService>();
    })
    .Build()
    .RunAsync();
