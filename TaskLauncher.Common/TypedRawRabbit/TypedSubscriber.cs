using Microsoft.Extensions.Options;
using RawRabbit;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace TaskLauncher.Common.TypedRawRabbit;

public interface ITypedSubscriber<TMessageContext> : IBusClient<TMessageContext> where TMessageContext : IMessageContext
{
    ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod);
}

public abstract class TypedSubscriber<TMessageContext, TSubscriberConfig>
    : BaseBusClient<TMessageContext>, ITypedSubscriber<TMessageContext>
    where TMessageContext : IMessageContext
    where TSubscriberConfig : BaseSubscriberConfig
{
    protected TSubscriberConfig SubscriberOptions { get; }

    public TypedSubscriber(IOptions<TSubscriberConfig> subscriberOptions,
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
        return SubscribeAsync(subscribeMethod, options =>
        {

        });
    }
}
