using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TaskLauncher.Worker.Services;

/// <summary>
/// Trida implementujici ITaskLauncherService
/// Spousti kontejnery
/// </summary>
public class TaskLauncherService : ITaskLauncherService
{
    public readonly DockerClient client;
    private readonly TaskLauncherConfig config;
    private readonly ILogger<TaskLauncherService> logger;

    public TaskLauncherService(IOptions<TaskLauncherConfig> config, ILogger<TaskLauncherService> logger)
    {
        //podle OS se vybere docker socket
        Uri uri = new("unix:///var/run/docker.sock");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            uri = new Uri("npipe://./pipe/docker_engine");
        }
        client = new DockerClientConfiguration(uri).CreateClient();
        this.config = config.Value;
        this.logger = logger;
        logger.LogInformation("Using {0}", uri.ToString());
    }

    public async Task<StartContainerResult> StartContainer(CancellationToken token)
    {
        var container = await client.Containers.CreateContainerAsync(new CreateContainerParameters()
        {
            Image = config.ImageName,
            HostConfig = new HostConfig
            {
                Binds = new List<string>() { $"{config.Source}:/{config.Target}" }
            }
        }, token);
        logger.LogInformation("Container '{0}' was created", container.ID);

        var result = await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), token);
        if (result)
            logger.LogInformation("Container '{0}' started", container.ID);
        return new StartContainerResult(result, container.ID);
    }

    public async Task<long> WaitContainer(string containerId, CancellationToken token)
    {
        var tmp = await client.Containers.WaitContainerAsync(containerId, token);
        logger.LogInformation("Container '{0}' finished with status code '{1}'", containerId, tmp.StatusCode);
        return tmp.StatusCode;
    }
}
