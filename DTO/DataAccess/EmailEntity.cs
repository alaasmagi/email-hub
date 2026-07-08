using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess;

public class EmailEntity : BaseEntityWithMeta
{
    [MaxLength(64)]
    public string ServiceName { get; set; } = default!;
    [MaxLength(64)]
    public string EmailType { get; set; } = default!;
    [MaxLength(2048)]
    public string ToRecipients { get; set; } = default!;
    [MaxLength(64)]
    public string? ReplyTo { get; set; }
    [MaxLength(1024)]
    public string? Subject { get; set; }
    public EEmailStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
}