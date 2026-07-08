using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class SenderIdentityRepository : BaseRepository<SenderIdentity, SenderIdentityEntity, SenderIdentityEntityMapper>, ISenderIdentityRepository
{
    private readonly AppDbContext _repositoryDbContext;
    private readonly SenderIdentityEntityMapper _repositoryMapper;

    public SenderIdentityRepository(AppDbContext repositoryDbContext, SenderIdentityEntityMapper repositoryMapper)
        : base(repositoryDbContext, repositoryMapper)
    {
        _repositoryDbContext = repositoryDbContext;
        _repositoryMapper = repositoryMapper;
    }

    public async Task<IReadOnlyList<SenderIdentity>> GetAllForAdminAsync()
    {
        var entities = await _repositoryDbContext.SenderIdentities
            .AsNoTracking()
            .OrderBy(x => x.EmailType)
            .ThenBy(x => x.FromAddress)
            .ToListAsync();

        return _repositoryMapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<SenderIdentity?> GetForAdminAsync(Guid id)
    {
        var entity = await _repositoryDbContext.SenderIdentities
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return _repositoryMapper.Map(entity);
    }

    public async Task<SenderIdentity?> GetActiveAsync(Guid clientId, string emailType)
    {
        var entity = await _repositoryDbContext.SenderIdentities
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ClientId == clientId &&
                x.EmailType == emailType &&
                x.IsActive);

        return _repositoryMapper.Map(entity);
    }
}
