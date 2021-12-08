using Google.Cloud.Storage.V1;
using TaskLauncher.Common.Configuration;

namespace TaskLauncher.Common.Services;

public interface IFileStorageService
{
    Task DownloadFileAsync(string path, Stream stream);
    Task UploadFileAsync(string path, Stream stream);
    Task RemoveFileAsync(string path);
}

/// <summary>
/// Zabaleni StorageClient pro pristup do google bucket storage
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly StorageClient storageClient;
    private readonly StorageConfiguration configuration;

    public FileStorageService(StorageConfiguration configuration)
    {
        storageClient = StorageClient.Create(configuration.GoogleCredential);
        this.configuration = configuration;
    }

    public async Task DownloadFileAsync(string path, Stream stream)
    {
        await storageClient.DownloadObjectAsync(configuration.BucketName, path, stream);
        stream.Position = 0;
    }

    public async Task UploadFileAsync(string path, Stream stream)
    {
        await storageClient.UploadObjectAsync(configuration.BucketName, path, "text/plain", stream);
    }

    public async Task RemoveFileAsync(string path)
    {
        await storageClient.DeleteObjectAsync(configuration.BucketName, path);
    }
}
