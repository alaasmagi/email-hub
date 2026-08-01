namespace Contracts.External.Models;

public enum EmailDispatchOutcome
{
    /// <summary>Rendered and accepted by Brevo.</summary>
    Sent,

    /// <summary>This envelope id was already sent — idempotent no-op, do not send again.</summary>
    AlreadySent,

    /// <summary>Dropped before sending because the payload had already expired.</summary>
    Expired,

    /// <summary>Transient send failure — safe and worthwhile to retry (requeue).</summary>
    TransientFailure,

    /// <summary>Permanent failure (no template/branding, render failure, Brevo 4xx) — do not retry.</summary>
    PermanentFailure
}

public class EmailDispatchResult
{
    public required EmailDispatchOutcome Outcome { get; init; }
    public Guid? EmailId { get; init; }
    public string? Error { get; init; }

    public static EmailDispatchResult From(EmailDispatchOutcome outcome, Guid? emailId = null, string? error = null)
        => new() { Outcome = outcome, EmailId = emailId, Error = error };
}
