using TaskLauncher.App.DAL;
using TaskLauncher.Common;
using TaskLauncher.Common.Services;

namespace TaskLauncher.Routines.Routines;

/// <summary>
/// Definice rutinni prace, ktera jednou za cas smaze stare soubory
/// </summary>
public class FileDeletionRoutine : IRoutine
{
    private readonly IFileStorageService fileStorageService;
    private readonly AppDbContext dbContext;
    private readonly ILogger<AppDbContext> logger;

    public FileDeletionRoutine(IFileStorageService fileStorageService, AppDbContext dbContext, ILogger<AppDbContext> logger)
    {
        this.fileStorageService = fileStorageService;
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public void Perform()
    {
        logger.LogInformation("Starting file deletion routine");
        var config = dbContext.Configs.Single(i => i.Key == Constants.Configuration.FileRemovalRoutine);
        fileStorageService.RemoveFilesIfOlderThanAsync(int.Parse(config.Value)).Wait();
        logger.LogInformation("End of file deletion routine");
    }
}
