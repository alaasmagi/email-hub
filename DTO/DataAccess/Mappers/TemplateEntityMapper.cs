using Base.Contracts.DTO;
using Domain;

namespace DTO.DataAccess.Mappers;

public class TemplateEntityMapper : IMapper<Template, TemplateEntity>
{
    public Template? Map(TemplateEntity? entity)
    {
        return entity == null
            ? null
            : new Template
            {
                Id = entity.Id,
                SenderIdentityId = entity.SenderIdentityId,
                LanguageCode = entity.LanguageCode,
                SubjectTemplate = entity.SubjectTemplate,
                HtmlBodyTemplate = entity.HtmlBodyTemplate,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<Template>? Map(IEnumerable<TemplateEntity>? entities)
    {
        return entities?.Select(Map).OfType<Template>().ToList();
    }

    public TemplateEntity? Map(Template? entity)
    {
        return entity == null
            ? null
            : new TemplateEntity
            {
                Id = entity.Id,
                SenderIdentityId = entity.SenderIdentityId,
                LanguageCode = entity.LanguageCode,
                SubjectTemplate = entity.SubjectTemplate,
                HtmlBodyTemplate = entity.HtmlBodyTemplate,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<TemplateEntity>? Map(IEnumerable<Template>? entities)
    {
        return entities?.Select(Map).OfType<TemplateEntity>().ToList();
    }
}