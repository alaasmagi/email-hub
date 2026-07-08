using Contracts.External.Models;

namespace Contracts.External;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        OutboundEmailMessage message,
        CancellationToken cancellationToken = default);
}
