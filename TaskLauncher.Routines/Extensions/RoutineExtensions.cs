using TaskLauncher.Routines.Routines;

namespace TaskLauncher.Routines.Extensions;

/// <summary>
/// Pridani rutinnich praci do DI
/// </summary>
public static class RoutineExtensions
{
    public static void AddRoutines<TAssembly>(this IServiceCollection services)
    {
        services.Scan(selector =>
            selector.FromAssemblyOf<TAssembly>()
            .AddClasses(classes => classes.AssignableTo(typeof(IRoutine)))
            .AsSelf()
            .WithScopedLifetime());
    }
}
