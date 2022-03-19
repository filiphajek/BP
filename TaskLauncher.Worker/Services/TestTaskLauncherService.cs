namespace TaskLauncher.Worker.Services;

/// <summary>
/// Trida implementujici ITaskLauncherService, ktera simuluje spousteni kontejneru
/// </summary>
public class TestTaskLauncherService : ITaskLauncherService
{
    private readonly Random random = new(DateTime.Now.Millisecond);

    public async Task<StartContainerResult> StartContainer(CancellationToken token)
    {
        await Task.Delay(2500, token);
        return new StartContainerResult(true, random.Next().ToString());
    }

    public Task<long> WaitContainer(string containerId, CancellationToken token)
    {
        return Task.FromResult((long)random.Next(0, 2000));
    }
}
