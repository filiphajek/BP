using TaskLauncher.Routines.Routines;

namespace TaskLauncher.Routines.Extensions;

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
