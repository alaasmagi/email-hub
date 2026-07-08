using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class ClientRepository : BaseRepository<Client, ClientEntity, ClientEntityMapper>, IClientRepository
{
    private readonly AppDbContext _repositoryDbContext;
    private readonly ClientEntityMapper _repositoryMapper;

    public ClientRepository(AppDbContext repositoryDbContext, ClientEntityMapper repositoryMapper)
        : base(repositoryDbContext, repositoryMapper)
    {
        _repositoryDbContext = repositoryDbContext;
        _repositoryMapper = repositoryMapper;
    }

    public async Task<IReadOnlyList<Client>> GetAllForAdminAsync()
    {
        var entities = await _repositoryDbContext.Clients
            .AsNoTracking()
            .OrderBy(x => x.ServiceName)
            .ToListAsync();

        return _repositoryMapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<Client?> GetForAdminAsync(Guid id)
    {
        var entity = await _repositoryDbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return _repositoryMapper.Map(entity);
    }

    public async Task<Client?> GetActiveByServiceNameAsync(string serviceName)
    {
        var entity = await _repositoryDbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ServiceName == serviceName &&
                x.IsActive);

        return _repositoryMapper.Map(entity);
    }
}
