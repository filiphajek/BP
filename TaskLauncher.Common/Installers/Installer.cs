using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaskLauncher.Common.Installers;

public abstract class Installer<TAssembly, TAssign> : IInstaller
{
    public virtual void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.Scan(selector =>
            selector.FromAssemblyOf<TAssembly>()
            .AddClasses(classes => classes.AssignableTo(typeof(TAssign)))
            .AsMatchingInterface()
            .WithScopedLifetime());
    }

    public virtual void Install(IServiceCollection services, Action<Scrutor.ITypeSelector> action) => services.Scan(action);
}
