using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace TaskLauncher.Common.RawRabbit;

public interface IDefaultRabbitMQClient : IBaseRabbitMQClient<MessageContext>
{
}

public class DefaultRabbitMQClient : BaseRabbitMQClient<MessageContext>, IDefaultRabbitMQClient
{
    public DefaultRabbitMQClient(IRawRabbitConfigurationBuilder configurationBuilder, 
        IConfigurationEvaluator configEval, 
        ISubscriber<MessageContext> subscriber, 
        IPublisher publisher, 
        IResponder<MessageContext> responder, 
        IRequester requester) 
        : base(configurationBuilder, configEval, subscriber, publisher, responder, requester)
    {
    }
}