using RawRabbit.Context;
using RawRabbit.Extensions.BulkGet;
using RawRabbit.Extensions.BulkGet.Model;
using RawRabbit.Extensions.Client;

namespace TaskLauncher.ContainerLauncher.Queue;

public class MessageQueue
{
    private readonly IBusClient client;
    private readonly int size;

    public string Queue { get; }

    public MessageQueue(IBusClient client, string queue, int size = 1)
    {
        this.client = client;
        Queue = queue;
        this.size = size;
    }

    public BulkMessage<T, MessageContext>? GetMessage<T>() where T : new()
    {
        return client.GetMessages(config =>
        {
            config.ForMessage<T>(msg => msg
                .FromQueues(Queue)
                .WithBatchSize(size)
            );
        }).GetMessages<T>().SingleOrDefault();
    }
}
