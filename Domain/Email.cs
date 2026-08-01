using Base.Domain;

namespace Domain;

public class Email : BaseEntity
{
    /// <summary>
    /// The envelope <c>id</c> of the command that produced this email. Unique — this is the
    /// idempotency key that makes at-least-once delivery safe: a duplicate delivery finds the
    /// existing row and is acked without sending again.
    /// </summary>
    public string MessageId { get; set; } = default!;

    /// <summary>Publisher of the command (envelope <c>source</c>, e.g. <c>identity-hub</c>).</summary>
    public string ServiceName { get; set; } = default!;

    /// <summary>Tenant whose data the command concerns (envelope <c>tenant</c>).</summary>
    public string Tenant { get; set; } = default!;

    public string EmailType { get; set; } = default!;
    public string ToRecipients { get; set; } = default!;
    public string? ReplyTo { get; set; }
    public string? Subject { get; set; }
    public EEmailStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
}