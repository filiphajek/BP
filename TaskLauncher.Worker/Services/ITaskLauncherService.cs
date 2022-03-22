namespace TaskLauncher.Worker.Services;

/// <summary>
/// Sluzba spoustejici kontejnery
/// </summary>
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
