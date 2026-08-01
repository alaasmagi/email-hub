namespace Contracts.External.Models;

/// <summary>
/// Per-tenant branding exposed to templates. Selected by <c>tenant</c>, independent of which template
/// (<c>{source}.{action}</c>) and which language (<c>content.locale</c>).
///
/// Only the from-identity fields are modelled today (they already existed on the sender identity, now
/// tenant-scoped). Logo, colours and footer are a known open item in the contract (§10, "tenant →
/// branding map … Not yet specified") and would be added as further tenant-keyed columns.
/// </summary>
public class EmailBranding
{
    public string Tenant { get; init; } = default!;
    public string FromAddress { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string? ReplyTo { get; init; }
}
