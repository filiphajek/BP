namespace TaskLauncher.Common.TypedRawRabbit;

public abstract class BasePublisherConfig
{
    public string RoutingKey { get; set; }
    public string ExchangeName { get; set; }
}

