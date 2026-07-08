namespace Contracts.External.Models;

public class OutboundEmailMessage
{
    public string FromAddress { get; set; } = default!;
    public string FromDisplayName { get; set; } = default!;
    public string? ReplyTo { get; set; }

    public string ToEmail { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string HtmlBody { get; set; } = default!;

    public string? CorrelationId { get; set; }
}