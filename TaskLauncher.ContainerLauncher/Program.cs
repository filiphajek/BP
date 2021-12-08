using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Services;
using Microsoft.Extensions.Configuration;
using TaskLauncher.ContainerLauncher;
using TaskLauncher.ContainerLauncher.Workers;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .AddJsonFile("appsettings.json")
    .Build();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddConfiguration(configuration);
    })
    .ConfigureServices(services =>
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
        services.AddSingleton(services =>
        {
            var config = services.GetRequiredService<IConfiguration>();
            var tmp = new Auth0Configuration();
            config.Bind(nameof(Auth0Configuration), tmp);
            return tmp;
        });

        //google bucket config
        services.AddSingleton(services =>
        {
            var config = services.GetRequiredService<IConfiguration>();
            var tmp = new StorageConfiguration();
            config.Bind(nameof(StorageConfiguration), tmp);
            return tmp;
        });

        //addresses to services
        services.AddSingleton(services =>
        {
            var config = services.GetRequiredService<IConfiguration>();
            var tmp = new ServiceAddresses();
            config.Bind(nameof(ServiceAddresses), tmp);
            return tmp;
        }); //WebApiAddress = "https://tasklauncher.app.api"

        //docker config
        services.AddSingleton(services =>
        {
            var config = services.GetRequiredService<IConfiguration>();
            var tmp = new TaskLauncherConfig();
            config.Bind(nameof(TaskLauncherConfig), tmp);
            return tmp;
        });

        //main worker thread
        services.AddHostedService<LauncherWorker>();
    }).Build();

await host.RunAsync();
