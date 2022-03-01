using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Context;

namespace TaskLauncher.Common.TypedRawRabbit;

public static class TypedRawRabbitExtensions
{
    public static void InstallTypedRawRabbit<TAssembly>(this IServiceCollection services)
    {
        services.AddScoped<IDefaultPublisher, DefaultPublisher>();
        services.AddScoped<IDefaultPublisher<MessageContext>, DefaultPublisher<MessageContext>>();
        services.AddScoped<IDefaultSubscriber, DefaultSubscriber>();
        services.AddScoped<IDefaultSubscriber<MessageContext>, DefaultSubscriber<MessageContext>>();
    }
}
