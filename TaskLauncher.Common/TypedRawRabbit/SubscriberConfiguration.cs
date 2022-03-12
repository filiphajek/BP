using RawRabbit.Configuration.Exchange;

namespace TaskLauncher.Common.TypedRawRabbit;

public class SubscriberConfiguration
{
    public string RoutingKey { get; set; }
    public string ExchangeName { get; set; }
    public string QueueName { get; set; }
    public int PrefetchCount { get; set; } = 1;
    public List<string> MessageTypes { get; set; } = new();
    public ExchangeType ExchangeType { get; set; } = ExchangeType.Direct;
}

