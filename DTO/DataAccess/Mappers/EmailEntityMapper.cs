using Base.Contracts.DTO;
using Domain;

namespace DTO.DataAccess.Mappers;

public class EmailEntityMapper : IMapper<Email, EmailEntity>
{
    public Email? Map(EmailEntity? entity)
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
                SentAt = entity.SentAt
            };
    }

    public IEnumerable<Email>? Map(IEnumerable<EmailEntity>? entities)
    {
        return entities?.Select(Map).OfType<Email>().ToList();
    }

    public EmailEntity? Map(Email? entity)
    {
        return entity == null
            ? null
            : new EmailEntity
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

    public IEnumerable<EmailEntity>? Map(IEnumerable<Email>? entities)
    {
        return entities?.Select(Map).OfType<EmailEntity>().ToList();
    }
}