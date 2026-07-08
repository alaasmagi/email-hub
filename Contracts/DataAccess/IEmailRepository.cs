using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IEmailRepository : IBaseRepository<Email>
{
    Task<IReadOnlyList<Email>> GetAllForAdminAsync();
    Task<Email?> GetForAdminAsync(Guid id);
}
