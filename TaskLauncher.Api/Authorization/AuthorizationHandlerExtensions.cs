using Microsoft.AspNetCore.Authorization;

namespace TaskLauncher.Api.Authorization;

public static class AuthorizationHandlerExtensions
{
    public static void AddAuthorizationHandlers<TAssembly>(this IServiceCollection services)
    {
        var requirements = typeof(TAssembly).Assembly.ExportedTypes
            .Where(type => type.IsAssignableTo(typeof(IAuthorizationRequirement)))
            .Where(type => type.IsSealed);

        var handler = typeof(PermissionHandler<>);
        foreach (var requirement in requirements)
        {
            var genHandler = handler.MakeGenericType(new[] { requirement });
            services.AddSingleton(typeof(IAuthorizationHandler), genHandler);
        }
    }
}
