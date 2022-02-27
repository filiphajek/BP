namespace TaskLauncher.Common.TypedRawRabbit;

public abstract class BaseSubscriberConfig
{
    public string RoutingKey { get; set; }
    public string ExchangeName { get; set; }
}

