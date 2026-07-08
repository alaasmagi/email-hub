using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface ITemplateRepository : IBaseRepository<Template>
{
    Task<IReadOnlyList<Template>> GetAllForAdminAsync();
    Task<Template?> GetForAdminAsync(Guid id);
    Task<Template?> GetActiveAsync(Guid senderIdentityId, string languageCode);
}
