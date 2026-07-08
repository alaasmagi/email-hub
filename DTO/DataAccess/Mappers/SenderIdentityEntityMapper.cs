using Base.Contracts.DTO;
using Domain;

namespace DTO.DataAccess.Mappers;

public class SenderIdentityEntityMapper : IMapper<SenderIdentity, SenderIdentityEntity>
{
    public SenderIdentity? Map(SenderIdentityEntity? entity)
    {
        return entity == null
            ? null
            : new SenderIdentity
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                EmailType = entity.EmailType,
                FromAddress = entity.FromAddress,
                DisplayName = entity.DisplayName,
                ReplyTo = entity.ReplyTo,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<SenderIdentity>? Map(IEnumerable<SenderIdentityEntity>? entities)
    {
        return entities?.Select(Map).OfType<SenderIdentity>().ToList();
    }

    public SenderIdentityEntity? Map(SenderIdentity? entity)
    {
        return entity == null
            ? null
            : new SenderIdentityEntity
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                EmailType = entity.EmailType,
                FromAddress = entity.FromAddress,
                DisplayName = entity.DisplayName,
                ReplyTo = entity.ReplyTo,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<SenderIdentityEntity>? Map(IEnumerable<SenderIdentity>? entities)
    {
        return entities?.Select(Map).OfType<SenderIdentityEntity>().ToList();
    }
}