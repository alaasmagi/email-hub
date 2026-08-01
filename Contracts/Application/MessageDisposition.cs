namespace Contracts.Application;

/// <summary>
/// What the consumer should tell the broker after processing a delivery. The consumer maps these to
/// AMQP operations; nothing else in the pipeline touches the channel.
/// </summary>
public enum MessageDisposition
{
    /// <summary>Handled (sent, dropped-as-expired, or a silent duplicate) → <c>BasicAck</c>.</summary>
    Ack,

    /// <summary>
    /// Permanently un-processable (validation failure, permanent send failure). Retrying cannot help,
    /// so <c>BasicNack(requeue: false)</c> — the broker dead-letters it for a human to see.
    /// </summary>
    RejectNoRequeue,

    /// <summary>
    /// Transient failure (network, Brevo 5xx, rate limit). <c>BasicNack(requeue: true)</c>; the queue's
    /// own <c>x-delivery-limit</c> dead-letters it after a few attempts. No app-side retry loop.
    /// </summary>
    RequeueTransient
}
