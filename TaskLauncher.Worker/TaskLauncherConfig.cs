namespace TaskLauncher.Worker;

public class TaskLauncherConfig
{
    public string Target { get; set; }
    public string Source { get; set; }
    public string ImageName { get; set; }
    public List<string> ContainerArguments { get; set; } = new();
}
