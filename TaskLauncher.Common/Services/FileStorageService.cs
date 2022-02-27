using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using TaskLauncher.Common.Configuration;

namespace TaskLauncher.Common.Services;

public interface IFileStorageService
{
    Task DownloadFileAsync(string path, Stream stream);
    Task UploadFileAsync(string path, Stream stream);
    Task RemoveFileAsync(string path);
    Task RemoveFilesIfOlderThanAsync(int days);
}

/// <summary>
/// Zabaleni StorageClient pro pristup do google bucket storage
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly StorageClient storageClient;
    private readonly StorageConfiguration configuration;

    public FileStorageService(IOptions<StorageConfiguration> configuration)
    {
        storageClient = StorageClient.Create(configuration.Value.GoogleCredential);
        this.configuration = configuration.Value;
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

    public async Task RemoveFilesIfOlderThanAsync(int days)
    {
        await foreach (var item in storageClient.ListObjectsAsync(configuration.BucketName))
        {
            Console.WriteLine(item.Name);
            if (!item.TimeCreated.HasValue)
                continue;

            var tmp = DateTime.Now - item.TimeCreated.Value;
            if (tmp.Days > days)
            {
                await storageClient.DeleteObjectAsync(item);
            }
        }
    }
}
