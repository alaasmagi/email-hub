using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.DataAccess;

public class SenderIdentityEntity : BaseEntityWithMeta
{
    public Guid ClientId { get; set; }
    [MaxLength(64)]
    public string EmailType { get; set; } = default!;
    [MaxLength(64)]
    public string Tenant { get; set; } = string.Empty;
    [MaxLength(64)]
    public string FromAddress { get; set; } = default!; 
    [MaxLength(64)]
    public string DisplayName { get; set; } = default!;
    [MaxLength(64)]
    public string? ReplyTo { get; set; }
    public bool IsActive { get; set; } = true;
}