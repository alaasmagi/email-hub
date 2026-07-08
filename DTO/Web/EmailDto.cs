using Base.Domain;
using Domain;

namespace DTO.Web;

public class EmailDto : BaseEntity
{
    public string ServiceName { get; set; } = default!;
    public string EmailType { get; set; } = default!;
    public string ToRecipients { get; set; } = default!;
    public string? ReplyTo { get; set; }
    public string? Subject { get; set; }
    public EEmailStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
}