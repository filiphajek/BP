using TaskLauncher.App.Server.Proxy;

namespace TaskLauncher.App.Server.Extensions;

public static class ProxyMiddlewareExtensions
{
    public static void AddProxyMiddlewares(this IServiceCollection services, IConfiguration configuration)
    {
        services.Scan(selector =>
            selector.FromAssemblyOf<ProxyMiddleware>()
            .AddClasses(classes => classes.AssignableTo(typeof(ProxyMiddleware)))
            .AsSelf()
            .WithScopedLifetime());
    }

    public static IApplicationBuilder UseProxyMiddlewares<TAssembly>(this IApplicationBuilder builder)
    {
        var assembly = typeof(TAssembly).Assembly;
        var middlewares = assembly.ExportedTypes
            .Where(type => type.IsAssignableTo(typeof(ProxyMiddleware)))
            .Where(type => !type.IsAbstract);

        foreach (var item in middlewares)
        {
            builder.UseMiddleware(item);
        }

        return builder;
    }
}
