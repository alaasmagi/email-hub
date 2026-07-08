using Base.Contracts.DTO;
using Domain;

namespace DTO.Web.Mappers;

public class ClientDtoMapper : IMapper<ClientDto, Client>
{
    public ClientDto? Map(Client? entity)
    {
        return entity == null
            ? null
            : new ClientDto
            {
                Id = entity.Id,
                ServiceName = entity.ServiceName,
                DisplayName = entity.DisplayName,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<ClientDto>? Map(IEnumerable<Client>? entities)
    {
        return entities?.Select(Map).OfType<ClientDto>().ToList();
    }

    public Client? Map(ClientDto? entity)
    {
        return entity == null
            ? null
            : new Client
            {
                Id = entity.Id,
                ServiceName = entity.ServiceName,
                DisplayName = entity.DisplayName,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<Client>? Map(IEnumerable<ClientDto>? entities)
    {
        return entities?.Select(Map).OfType<Client>().ToList();
    }
}