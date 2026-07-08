using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IClientRepository : IBaseRepository<Client>
{
    Task<IReadOnlyList<Client>> GetAllForAdminAsync();
    Task<Client?> GetForAdminAsync(Guid id);
    Task<Client?> GetActiveByServiceNameAsync(string serviceName);
}
