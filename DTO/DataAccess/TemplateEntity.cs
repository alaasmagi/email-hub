using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.DataAccess;

public class TemplateEntity : BaseEntityWithMeta
{
    public Guid SenderIdentityId { get; set; }
    [MaxLength(10)]
    public string LanguageCode { get; set; } = "en";    
    [MaxLength(1024)]
    public string SubjectTemplate { get; set; } = default!;
    [MaxLength(524288)] 
    public string HtmlBodyTemplate { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}