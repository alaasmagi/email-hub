namespace Contracts.Application;

/// <summary>
/// Processes one raw delivery from the email command queue and decides its disposition. Owns all of
/// the contract's consumer rules — routing-key validation, contentVersion check, idempotency, expiry —
/// and returns what the broker should be told. Deliberately transport-agnostic: it takes the routing
/// key and the raw body, not a RabbitMQ type, so the AMQP concerns stay in the consumer.
/// </summary>
public interface IEmailCommandProcessor
{
    Task<MessageDisposition> ProcessAsync(
        string routingKey,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default);
}
