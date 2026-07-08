using Base.Domain;

namespace DTO.Web;

public class SenderIdentityDto : BaseEntity
{
    public Guid ClientId { get; set; }
    public string EmailType { get; set; } = default!;
    public string FromAddress { get; set; } = default!; 
    public string DisplayName { get; set; } = default!;
    public string? ReplyTo { get; set; }
    public bool IsActive { get; set; } = true;
}