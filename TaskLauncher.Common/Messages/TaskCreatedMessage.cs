namespace TaskLauncher.Common.Messages;

public class TaskCreatedMessage
{
    public string Value { get; set; }
}

public class ConfigChangedMessage
{
    public string Name { get; set; }
    public string Value { get; set; }
}
