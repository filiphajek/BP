namespace TaskLauncher.Worker;

public class TaskLauncherConfig
{
    public string Target { get; set; }
    public string Source { get; set; }
    public string ImageName { get; set; }
    public ContainerArguments ContainerArguments { get; set; }
}

public class ContainerArguments
{
    public string Mode { get; set; }
    public int Max { get; set; }
    public int Min { get; set; }
    public string Chance { get; set; }

    public List<string> GetArgumentsList()
    {
        return new List<string>
        {
            Mode,
            "--min",
            Min.ToString(),
            "--max",
            Max.ToString(),
            "--chance",
            Chance,
        };
    }
}