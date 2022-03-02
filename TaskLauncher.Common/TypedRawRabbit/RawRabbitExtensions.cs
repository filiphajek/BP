using Microsoft.Extensions.DependencyInjection;

namespace TaskLauncher.Common.RawRabbit;

public static class RawRabbitExtensions
{
    public static void InstallRawRabbitExtensions(this IServiceCollection services)
    {
        services.AddSingleton<IRawRabbitConfigurationBuilder, RawRabbitConfigurationBuilder>();
        services.AddScoped<IDefaultRabbitMQClient, DefaultRabbitMQClient>();
    }
}
