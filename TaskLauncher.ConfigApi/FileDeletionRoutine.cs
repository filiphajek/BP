using TaskLauncher.Common.Services;

namespace TaskLauncher.ConfigApi;

public class FileDeletionRoutine
{
    private readonly IFileStorageService fileStorageService;
    private readonly IConfigFileEditor config;

    public FileDeletionRoutine(IFileStorageService fileStorageService, IConfigFileEditor config)
    {
        this.fileStorageService = fileStorageService;
        this.config = config;
    }

    public void Handle()
    {
        //int.Parse(config.GetValue("autofileremove"))
        var tmp = config.GetValue("autofileremove");
        fileStorageService.RemoveFilesIfOlderThanAsync(100).Wait();
    }
}
