using Microsoft.Extensions.Options;
using RawRabbit;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace TaskLauncher.Common.TypedRawRabbit;

public interface IDefaultSubscriber : IDefaultSubscriber<MessageContext>
{
}

public interface IDefaultSubscriber<TMessageContext> : IBusClient<TMessageContext> 
    where TMessageContext : IMessageContext
{
    ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod);
}

public class DefaultSubscriber<TMessageContext> : BaseBusClient<TMessageContext>, IDefaultSubscriber<TMessageContext>
    where TMessageContext : IMessageContext
{
    protected SubscriberConfiguration SubscriberOptions { get; }

    public DefaultSubscriber(IOptions<SubscriberConfiguration> subscriberOptions,
        IConfigurationEvaluator configEval,
        ISubscriber<TMessageContext> subscriber,
        IPublisher publisher,
        IResponder<TMessageContext> responder,
        IRequester requester)
        : base(configEval, subscriber, publisher, responder, requester)
    {
        SubscriberOptions = subscriberOptions.Value;
    }

    public virtual ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod)
    {
        return SubscribeAsync(subscribeMethod, config =>
        {
            config.WithExchange(exchange =>
            {
                exchange.WithName(SubscriberOptions.ExchangeName);
            });
            config.WithRoutingKey(SubscriberOptions.RoutingKey);
            config.WithQueue(queue =>
            {
                queue.WithName(SubscriberOptions.QueueName);
            });
            config.WithPrefetchCount((ushort)SubscriberOptions.PrefetchCount);
        });
    }
}

public class DefaultSubscriber : DefaultSubscriber<MessageContext>, IDefaultSubscriber
{
    public DefaultSubscriber(IOptions<SubscriberConfiguration> subscriberOptions, 
        IConfigurationEvaluator configEval, 
        ISubscriber<MessageContext> subscriber, 
        IPublisher publisher, 
        IResponder<MessageContext> responder, 
        IRequester requester) 
        : base(subscriberOptions, configEval, subscriber, publisher, responder, requester)
    {
    }
}