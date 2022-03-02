using Microsoft.Extensions.Options;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Subscribe;

namespace TaskLauncher.Common.RawRabbit;

public interface IRawRabbitConfigurationBuilder
{
    Action<IPublishConfigurationBuilder>? GetPublishConfigurationFor<TMessage>();
    Action<ISubscriptionConfigurationBuilder>? GetSubscribeConfigurationFor<TMessage>();
}

public class RawRabbitConfigurationBuilder : IRawRabbitConfigurationBuilder
{
    private readonly RawRabbitConfiguration options;

    public RawRabbitConfigurationBuilder(IOptions<RawRabbitConfiguration> options)
    {
        this.options = options.Value;
    }

    public Action<IPublishConfigurationBuilder>? GetPublishConfigurationFor<TMessage>()
    {
        var name = typeof(TMessage).Name;
        var publishConfig = options.Publish.SingleOrDefault(i => i.MessageTypes.Contains(name));

        if (publishConfig is null)
        {
            publishConfig = options.Publish.SingleOrDefault(i => !i.MessageTypes.Any());
            if(publishConfig is null)
                return null;
        }

        return config =>
        {
            config.WithRoutingKey(publishConfig.RoutingKey);
            config.WithExchange(exchange =>
            {
                exchange.WithType(publishConfig.ExchangeType);
                exchange.WithName(publishConfig.ExchangeName);
            });
        };
    }

    public Action<ISubscriptionConfigurationBuilder>? GetSubscribeConfigurationFor<TMessage>()
    {
        var name = nameof(TMessage);
        var subscribeConfig = options.Subscribe.SingleOrDefault(i => i.MessageTypes.Contains(name));

        if (subscribeConfig is null)
        {
            subscribeConfig = options.Subscribe.SingleOrDefault(i => !i.MessageTypes.Any());
            if (subscribeConfig is null)
                return null;
        }

        return config =>
        {
            config.WithExchange(exchange =>
            {
                exchange.WithType(subscribeConfig.ExchangeType);
                exchange.WithName(subscribeConfig.ExchangeName);
            });
            config.WithRoutingKey(subscribeConfig.RoutingKey);
            config.WithQueue(queue =>
            {
                queue.WithName(subscribeConfig.QueueName);
            });
            config.WithPrefetchCount((ushort)subscribeConfig.PrefetchCount);
        };
    }
}