using Base.Contracts.DTO;
using Domain;

namespace DTO.DataAccess.Mappers;

public class ClientEntityMapper : IMapper<Client, ClientEntity>
{
    public Client? Map(ClientEntity? entity)
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

    public IEnumerable<Client>? Map(IEnumerable<ClientEntity>? entities)
    {
        return entities?.Select(Map).OfType<Client>().ToList();
    }

    public ClientEntity? Map(Client? entity)
    {
        return entity == null
            ? null
            : new ClientEntity
            {
                Id = entity.Id,
                ServiceName = entity.ServiceName,
                DisplayName = entity.DisplayName,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<ClientEntity>? Map(IEnumerable<Client>? entities)
    {
        return entities?.Select(Map).OfType<ClientEntity>().ToList();
    }
}