using Base.Domain;

namespace DTO.Web;

public class TemplateDto : BaseEntity
{
    public Guid SenderIdentityId { get; set; }
    public string LanguageCode { get; set; } = "en";    
    public string SubjectTemplate { get; set; } = default!;
    public string HtmlBodyTemplate { get; set; } = default!;
    public bool IsActive { get; set; }
}