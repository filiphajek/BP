namespace TaskLauncher.Worker.Services;

/// <summary>
/// Vysledek operace spusteni kontejneru
/// </summary>
public record StartContainerResult(bool Success, string ContainerId);
