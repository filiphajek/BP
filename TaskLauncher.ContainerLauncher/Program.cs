using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Services;
using TaskLauncher.ContainerLauncher;
using TaskLauncher.ContainerLauncher.Queue;
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
        services.AddSingleton<TokenProvider>();

        //auth0 config
        services.Configure<Auth0Configuration>(context.Configuration.GetSection(nameof(Auth0Configuration)));
        //google bucket config
        services.Configure<StorageConfiguration>(context.Configuration.GetSection(nameof(StorageConfiguration)));
        //api addresses
        services.Configure<ServiceAddresses>(context.Configuration.GetSection(nameof(ServiceAddresses)));
        //docker config
        services.Configure<TaskLauncherConfig>(context.Configuration.GetSection(nameof(TaskLauncherConfig)));
        //rawrabbit
        services.Configure<QueuesPriorityConfiguration>(context.Configuration.GetSection("RawRabbit"));
        services.AddRawRabbit(cfg => cfg.AddJsonFile("rawrabbit.json"));
        services.AddRawRabbitExtensions<MessageContext>();

        //main worker thread
        services.AddHostedService<LauncherWorker>();
    })
    .Build()
    .RunAsync();
