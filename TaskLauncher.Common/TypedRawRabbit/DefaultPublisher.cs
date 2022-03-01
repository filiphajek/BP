using Microsoft.Extensions.Options;
using RawRabbit;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace TaskLauncher.Common.TypedRawRabbit;

public interface IDefaultPublisher : IDefaultPublisher<MessageContext>
{
}

public interface IDefaultPublisher<TMessageContext> : IBusClient<TMessageContext> where TMessageContext : IMessageContext
{
    Task PublishAsync<T>(T message);
}

public class DefaultPublisher<TMessageContext> : BaseBusClient<TMessageContext>, IDefaultPublisher<TMessageContext>
    where TMessageContext : IMessageContext
{
    protected PublisherConfiguration PublisherOptions { get; }

    public DefaultPublisher(IOptions<PublisherConfiguration> publisherOptions,
        IConfigurationEvaluator configEval,
        ISubscriber<TMessageContext> subscriber,
        IPublisher publisher,
        IResponder<TMessageContext> responder,
        IRequester requester)
        : base(configEval, subscriber, publisher, responder, requester)
    {
        PublisherOptions = publisherOptions.Value;
    }

    public virtual async Task PublishAsync<T>(T message)
    {
        await PublishAsync(message, configuration: config =>
        {
            config.WithRoutingKey(PublisherOptions.RoutingKey);
            config.WithExchange(exchange =>
            {
                exchange.WithName(PublisherOptions.ExchangeName);
            });
        });
    }
}

public class DefaultPublisher : DefaultPublisher<MessageContext>, IDefaultPublisher
{
    protected DefaultPublisher(IOptions<PublisherConfiguration> publisherOptions, 
        IConfigurationEvaluator configEval, 
        ISubscriber<MessageContext> subscriber, 
        IPublisher publisher, 
        IResponder<MessageContext> responder, 
        IRequester requester) 
        : base(publisherOptions, configEval, subscriber, publisher, responder, requester)
    {
    }
}