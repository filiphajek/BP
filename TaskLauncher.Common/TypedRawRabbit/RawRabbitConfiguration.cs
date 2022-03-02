namespace TaskLauncher.Common.RawRabbit;

public class RawRabbitConfiguration
{
    public List<PublisherConfiguration> Publish { get; set; } = new();
    public List<SubscriberConfiguration> Subscribe { get; set; } = new();
}

