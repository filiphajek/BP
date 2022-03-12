using RawRabbit.Configuration.Exchange;

namespace TaskLauncher.Common.TypedRawRabbit;

public class PublisherConfiguration
{
    public ExchangeType ExchangeType { get; set; } = ExchangeType.Direct;
    public string RoutingKey { get; set; }
    public string ExchangeName { get; set; }
    public List<string> MessageTypes { get; set; } = new();
}

