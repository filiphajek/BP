namespace TaskLauncher.Worker;

/// <summary>
/// Konfigurace workera
/// </summary>
public class TaskLauncherConfig
{
    public string Target { get; set; }
    public string Source { get; set; }
    public string ImageName { get; set; }
    public ContainerArguments ContainerArguments { get; set; }
}
