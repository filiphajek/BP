using RawRabbit;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace TaskLauncher.Common.TypedRawRabbit;

public interface IBaseRabbitMQClient<TMessageContext> : IBusClient<TMessageContext>
    where TMessageContext : IMessageContext
{
    ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod);
    Task PublishAsync<T>(T message);
}

public class BaseRabbitMQClient<TMessageContext> : BaseBusClient<TMessageContext>, IBaseRabbitMQClient<TMessageContext>
    where TMessageContext : IMessageContext
{
    private readonly IRawRabbitConfigurationBuilder configurationBuilder;

    public BaseRabbitMQClient(IRawRabbitConfigurationBuilder configurationBuilder,
        IConfigurationEvaluator configEval,
        ISubscriber<TMessageContext> subscriber,
        IPublisher publisher,
        IResponder<TMessageContext> responder,
        IRequester requester)
        : base(configEval, subscriber, publisher, responder, requester)
    {
        this.configurationBuilder = configurationBuilder;
    }

    public virtual ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod)
    {
        return SubscribeAsync(subscribeMethod, configurationBuilder.GetSubscribeConfigurationFor<T>());
    }

    public virtual async Task PublishAsync<T>(T message)
    {
        await PublishAsync(message, configuration: configurationBuilder.GetPublishConfigurationFor<T>());
    }
}
