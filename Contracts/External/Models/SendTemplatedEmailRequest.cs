namespace Contracts.External.Models;

public class SendTemplatedEmailRequest
{
    /// <summary>Envelope id — the idempotency key. A row already sent for this id is not re-sent.</summary>
    public string MessageId { get; set; } = default!;

    /// <summary>Publisher (<c>source</c>) — the template namespace. Taken from the routing key.</summary>
    public string Source { get; set; } = default!;

    /// <summary>Tenant (<c>tenant</c>) — selects branding.</summary>
    public string Tenant { get; set; } = default!;

    /// <summary>Action (<c>{source}.{action}</c> selects the template; stored as the email type).</summary>
    public string EmailType { get; set; } = default!;

    public string ToEmail { get; set; } = default!;
    public string LanguageCode { get; set; } = "en";
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Strongly-typed event content payload used as the model when rendering the template.
    /// </summary>
    public object Content { get; set; } = default!;
}