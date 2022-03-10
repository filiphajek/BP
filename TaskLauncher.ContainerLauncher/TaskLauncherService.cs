using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;

namespace TaskLauncher.ContainerLauncher;

public interface ITaskLauncherService
{
    /// <summary>
    /// Vytvoreni a spusteni kontejneru s bind mountem
    /// </summary>
    Task<StartContainerResult> StartContainer(CancellationToken token);

    /// <summary>
    /// Ceka az spusteny kontejner skonci
    /// </summary>
    Task<long> WaitContainer(string containerId, CancellationToken token);
}

public record StartContainerResult(bool Success, string ContainerId);

public class TaskLauncherService : ITaskLauncherService
{
    public readonly DockerClient client;
    private readonly TaskLauncherConfig config;

    public TaskLauncherService(IOptions<TaskLauncherConfig> config)
    {
        Uri uri = new("unix:///var/run/docker.sock");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            uri = new Uri("npipe://./pipe/docker_engine");
        }
        client = new DockerClientConfiguration(uri).CreateClient();
        this.config = config.Value;
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
        });

        var result = await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), token);
        return new StartContainerResult(result, container.ID);
    }

    public async Task<long> WaitContainer(string containerId, CancellationToken token) 
        => (await client.Containers.WaitContainerAsync(containerId, token)).StatusCode;
}
