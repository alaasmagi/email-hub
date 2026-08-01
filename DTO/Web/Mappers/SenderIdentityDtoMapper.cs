using Base.Contracts.DTO;
using Domain;

namespace DTO.Web.Mappers;

public class SenderIdentityDtoMapper : IMapper<SenderIdentityDto, SenderIdentity>
{
    public SenderIdentityDto? Map(SenderIdentity? entity)
    {
        return entity == null
            ? null
            : new SenderIdentityDto
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                EmailType = entity.EmailType,
                Tenant = entity.Tenant,
                FromAddress = entity.FromAddress,
                DisplayName = entity.DisplayName,
                ReplyTo = entity.ReplyTo,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<SenderIdentityDto>? Map(IEnumerable<SenderIdentity>? entities)
    {
        return entities?.Select(Map).OfType<SenderIdentityDto>().ToList();
    }

    public SenderIdentity? Map(SenderIdentityDto? entity)
    {
        return entity == null
            ? null
            : new SenderIdentity
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                EmailType = entity.EmailType,
                Tenant = entity.Tenant,
                FromAddress = entity.FromAddress,
                DisplayName = entity.DisplayName,
                ReplyTo = entity.ReplyTo,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<SenderIdentity>? Map(IEnumerable<SenderIdentityDto>? entities)
    {
        return entities?.Select(Map).OfType<SenderIdentity>().ToList();
    }
}