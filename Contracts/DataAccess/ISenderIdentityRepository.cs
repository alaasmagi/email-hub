using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface ISenderIdentityRepository : IBaseRepository<SenderIdentity>
{
    Task<IReadOnlyList<SenderIdentity>> GetAllForAdminAsync();
    Task<SenderIdentity?> GetForAdminAsync(Guid id);
    Task<SenderIdentity?> GetActiveAsync(Guid clientId, string emailType);
}
