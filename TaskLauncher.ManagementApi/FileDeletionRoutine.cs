using TaskLauncher.Common.Services;

namespace TaskLauncher.ManagementApi;

public interface IRoutine
{
    void Perform();
}

public class FileDeletionRoutine : IRoutine
{
    private readonly IFileStorageService fileStorageService;
    private readonly IConfigurationFile config;

    public FileDeletionRoutine(IFileStorageService fileStorageService, IConfigurationFile config)
    {
        this.fileStorageService = fileStorageService;
        this.config = config;
    }

    public void Perform()
    {
        var tmp = config.GetValue("autofileremove");
        fileStorageService.RemoveFilesIfOlderThanAsync(int.Parse(tmp)).Wait();
    }
}

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