namespace TaskLauncher.Common.TypedRawRabbit;

public class SubscriberConfiguration
{
    public string RoutingKey { get; set; }
    public string ExchangeName { get; set; }
    public string QueueName { get; set; }
    public int PrefetchCount { get; set; }
}

