using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Services;
using TaskLauncher.Worker;
using TaskLauncher.Worker.Services;
using TaskLauncher.Worker.Workers;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddCommandLine(args);
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
        var isTest = context.Configuration.GetValue<bool>("test");

        //pokud byl zadan parametr test, worker se spusti v testovacim modu - nebude se spoustet kontejner
        if(isTest)
            services.AddSingleton<ITaskLauncherService, TestTaskLauncherService>();
        else
            services.AddSingleton<ITaskLauncherService, TaskLauncherService>();

        services.AddSingleton<IFileStorageService, FileStorageService>();
        services.AddSingleton<SignalRClient>();

        //token services with cache
        services.AddDistributedMemoryCache();
        services.AddSingleton(new CacheConfiguration<AccessToken> { AbsoluteExpiration = TimeSpan.FromHours(5) });
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
        services.AddHostedService<WorkerService>();
    })
    .Build()
    .RunAsync();
