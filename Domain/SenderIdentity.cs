using Base.Domain;

namespace Domain;

public class SenderIdentity : BaseEntity
{
    public Guid ClientId { get; set; }
    public string EmailType { get; set; } = default!;

    /// <summary>
    /// The tenant (realm/app slug) whose branding this sender identity carries. Empty string acts as
    /// the tenant-agnostic default that any tenant falls back to when no tenant-specific row exists.
    /// </summary>
    public string Tenant { get; set; } = string.Empty;
    public string FromAddress { get; set; } = default!; 
    public string DisplayName { get; set; } = default!;
    public string? ReplyTo { get; set; }
    public bool IsActive { get; set; } = true;
}