using TaskLauncher.Api.DAL;
using TaskLauncher.Common.Services;

namespace TaskLauncher.WebApp.Server.Routines;

public class FileDeletionRoutine : IRoutine
{
    private readonly IFileStorageService fileStorageService;
    private readonly AppDbContext dbContext;

    public FileDeletionRoutine(IFileStorageService fileStorageService, AppDbContext dbContext)
    {
        this.fileStorageService = fileStorageService;
        this.dbContext = dbContext;
    }

    public void Perform()
    {
        var config = dbContext.Configs.Single(i => i.Key == "autofileremove");
        fileStorageService.RemoveFilesIfOlderThanAsync(int.Parse(config.Value)).Wait();
    }
}
