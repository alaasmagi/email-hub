using Contracts.External.Models;

namespace Contracts.Application;

public interface IEmailDispatchService
{
    /// <summary>
    /// Renders and sends one templated email idempotently (keyed on <see cref="SendTemplatedEmailRequest.MessageId"/>).
    /// The outcome tells the caller how to dispose of the message: sent/already-sent → ack, transient →
    /// requeue, permanent → reject.
    /// </summary>
    Task<EmailDispatchResult> SendTemplatedEmailAsync(SendTemplatedEmailRequest request,
        CancellationToken cancellationToken = default);
}
