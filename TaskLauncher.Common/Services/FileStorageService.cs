using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using TaskLauncher.Common.Configuration;

namespace TaskLauncher.Common.Services;

/// <summary>
/// Interace, ktery se pouziva pro praci se soubory
/// </summary>
public interface IFileStorageService
{
    Task DownloadFileAsync(string path, Stream stream, CancellationToken cancellationToken = default);
    Task UploadFileAsync(string path, Stream stream, CancellationToken cancellationToken = default);
    Task RemoveFileAsync(string path, CancellationToken cancellationToken = default);
    Task RemoveFilesIfOlderThanAsync(int days, CancellationToken cancellationToken = default);
}

/// <summary>
/// Zabaleni StorageClient pro pristup do google bucket storage, implementuje IFileStorageService
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

    public async Task DownloadFileAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        await storageClient.DownloadObjectAsync(configuration.BucketName, path, stream, cancellationToken: cancellationToken);
        stream.Position = 0;
    }

    public async Task UploadFileAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        await storageClient.UploadObjectAsync(configuration.BucketName, path, "text/plain", stream, cancellationToken: cancellationToken);
    }

    public async Task RemoveFileAsync(string path, CancellationToken cancellationToken = default)
    {
        await storageClient.DeleteObjectAsync(configuration.BucketName, path, cancellationToken: cancellationToken);
    }

    public async Task RemoveFilesIfOlderThanAsync(int days, CancellationToken cancellationToken = default)
    {
        await foreach (var item in storageClient.ListObjectsAsync(configuration.BucketName))
        {
            if (!item.TimeCreated.HasValue)
                continue;

            var tmp = DateTime.Now - item.TimeCreated.Value;
            if (tmp.Days > days)
            {
                await storageClient.DeleteObjectAsync(item, cancellationToken: cancellationToken);
                Console.WriteLine("Removing " + item.Name);
            }
        }
    }
}
