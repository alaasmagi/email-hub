using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface ISenderIdentityRepository : IBaseRepository<SenderIdentity>
{
    Task<IReadOnlyList<SenderIdentity>> GetAllForAdminAsync();
    Task<SenderIdentity?> GetForAdminAsync(Guid id);
    /// <summary>
    /// Resolves the branding (sender identity) for a client + email type, preferring a row that
    /// matches <paramref name="tenant"/> and falling back to the tenant-agnostic default row
    /// (<c>Tenant == ""</c>) when none exists for the tenant.
    /// </summary>
    Task<SenderIdentity?> GetActiveAsync(Guid clientId, string emailType, string tenant);
}
