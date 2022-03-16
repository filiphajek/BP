namespace TaskLauncher.App.Server.Routines;

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