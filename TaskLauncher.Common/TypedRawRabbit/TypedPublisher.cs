using Microsoft.Extensions.Options;
using RawRabbit;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace TaskLauncher.Common.TypedRawRabbit;

public interface ITypedPublisher<TMessageContext> : IBusClient<TMessageContext> where TMessageContext : IMessageContext
{
    Task PublishAsync<T>(T message);
}

public abstract class TypedPublisher<TMessageContext, TPublisherConfig>
    : BaseBusClient<TMessageContext>, ITypedPublisher<TMessageContext>
    where TMessageContext : IMessageContext
    where TPublisherConfig : BasePublisherConfig
{
    protected TPublisherConfig PublisherOptions { get; }

    public TypedPublisher(IOptions<TPublisherConfig> publisherOptions,
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
