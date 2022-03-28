namespace TaskLauncher.Worker;

/// <summary>
/// Konfigurace kontejneru
/// </summary>
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