using Microsoft.Extensions.Options;
using TaskLauncher.Common.TypedRawRabbit;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace TaskLauncher.ManagementApi;

public class ConfigValueChanged : BasePublisherConfig { }

public interface IConfigPublisher : ITypedPublisher<MessageContext> { }

public class ConfigPublisher : TypedPublisher<MessageContext, ConfigValueChanged>, IConfigPublisher
{
    public ConfigPublisher(IOptions<ConfigValueChanged> publisherOptions,
        IConfigurationEvaluator configEval,
        ISubscriber<MessageContext> subscriber,
        IPublisher publisher,
        IResponder<MessageContext> responder,
        IRequester requester)
        : base(publisherOptions, configEval, subscriber, publisher, responder, requester)
    {
    }
}
