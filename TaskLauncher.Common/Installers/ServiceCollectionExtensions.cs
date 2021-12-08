using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaskLauncher.Common.Installers;

public static class ServiceCollectionExtensions
{
    public static void InstallServicesInAssemblyOf<T>(this IServiceCollection services, IConfiguration configuration)
    {
        typeof(T).Assembly.ExportedTypes
            .Where(x => typeof(IInstaller).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface)
            .Select(Activator.CreateInstance)
            .Where(x => x != null)
            .Cast<IInstaller>()
            .ToList()
            .ForEach(installer => installer.Install(services, configuration));
    }

    public static void InstallService<TInstaller>(this IServiceCollection serviceCollection, IConfiguration configuration)
        where TInstaller : IInstaller, new()
    {
        var installer = new TInstaller();
        installer.Install(serviceCollection, configuration);
    }
}