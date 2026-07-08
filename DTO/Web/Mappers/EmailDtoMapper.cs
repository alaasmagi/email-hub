using Base.Contracts.DTO;
using Domain;

namespace DTO.Web.Mappers;

public class EmailDtoMapper : IMapper<EmailDto, Email>
{
    public EmailDto? Map(Email? entity)
    {
        return entity == null
            ? null
            : new EmailDto
            {
                Id = entity.Id,
                ServiceName = entity.ServiceName,
                EmailType = entity.EmailType,
                ToRecipients = entity.ToRecipients,
                ReplyTo = entity.ReplyTo,
                Subject = entity.Subject,
                Status = entity.Status,
                SentAt = entity.SentAt
            };
    }

    public IEnumerable<EmailDto>? Map(IEnumerable<Email>? entities)
    {
        return entities?.Select(Map).OfType<EmailDto>().ToList();
    }

    public Email? Map(EmailDto? entity)
    {
        return entity == null
            ? null
            : new Email
            {
                Id = entity.Id,
                ServiceName = entity.ServiceName,
                EmailType = entity.EmailType,
                ToRecipients = entity.ToRecipients,
                ReplyTo = entity.ReplyTo,
                Subject = entity.Subject,
                Status = entity.Status,
                SentAt = entity.SentAt ?? default
            };
    }

    public IEnumerable<Email>? Map(IEnumerable<EmailDto>? entities)
    {
        return entities?.Select(Map).OfType<Email>().ToList();
    }
}