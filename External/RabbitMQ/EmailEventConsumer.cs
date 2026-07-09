using System.Text.Json;
using Base.Contracts.Message;
using Base.Message.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace External.RabbitMQ;

/// <summary>
/// Queue-only consumer: reads the configured queue and hands each envelope to the registered handler,
/// which branches on the envelope <c>action</c> to pick the concrete content payload. No routing-key
/// patterns are supplied, so the consumer never binds to or references an exchange — exchange/binding
/// topology is owned by the infrastructure, not this service.
/// </summary>
public class EmailEventConsumer : RabbitMqConsumerBase<JsonElement>
{
    public EmailEventConsumer(
        RabbitMqConnectionManager connections,
        RabbitMqOptions options,
        EmailQueueOptions queueOptions,
        IBaseEventHandler<JsonElement> handler,
        ILogger<EmailEventConsumer> logger)
        : base(connections, options, handler, logger, queueName: queueOptions.QueueName)
    {
    }
}

