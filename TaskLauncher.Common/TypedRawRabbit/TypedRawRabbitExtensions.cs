using Microsoft.Extensions.DependencyInjection;

namespace TaskLauncher.Common.TypedRawRabbit;

public static class TypedRawRabbitExtensions
{
    public static void InstallTypedRawRabbit<TAssembly>(this IServiceCollection services)
    {
        services
            .Scan(selector => selector.FromAssemblyOf<TAssembly>()
            .AddClasses(classes => classes.AssignableTo(typeof(ITypedSubscriber<>)))
            .AddClasses(classes => classes.AssignableTo(typeof(ITypedPublisher<>)))
            .AsMatchingInterface()
            .WithScopedLifetime());
    }
}
