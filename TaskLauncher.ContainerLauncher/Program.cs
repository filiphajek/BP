using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Services;
using TaskLauncher.ContainerLauncher;
using TaskLauncher.ContainerLauncher.Workers;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("rawrabbit.json");
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
        services.AddScoped(sp => new HttpClient(sp.GetRequiredService<HttpClientHandler>())
        {
            BaseAddress = sp.GetRequiredService<ServiceAddresses>().WebApiAddressUri
        });

        services.AddSingleton<IFileStorageService, FileStorageService>();
        services.AddSingleton<ITaskLauncherService, TaskLauncherService>();
        services.AddSingleton<SignalRClient>();

        //token services with cache
        services.AddDistributedMemoryCache();
        services.AddSingleton<Cache<AccessToken>>();
        services.AddSingleton<ManagementTokenService>();

        //auth0 config
        services.Configure<Auth0ApiConfiguration>(context.Configuration.GetSection(nameof(Auth0ApiConfiguration)));
        //google bucket config
        services.Configure<StorageConfiguration>(context.Configuration.GetSection(nameof(StorageConfiguration)));
        //api addresses
        services.Configure<ServiceAddresses>(context.Configuration.GetSection(nameof(ServiceAddresses)));
        //docker config
        services.Configure<TaskLauncherConfig>(context.Configuration.GetSection(nameof(TaskLauncherConfig)));

        //main worker thread
        services.AddHostedService<LauncherWorker>();
    })
    .Build()
    .RunAsync();
