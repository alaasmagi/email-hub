using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class TemplateRepository : BaseRepository<Template, TemplateEntity, TemplateEntityMapper>, ITemplateRepository
{
    private readonly AppDbContext _repositoryDbContext;
    private readonly TemplateEntityMapper _repositoryMapper;

    public TemplateRepository(AppDbContext repositoryDbContext, TemplateEntityMapper repositoryMapper)
        : base(repositoryDbContext, repositoryMapper)
    {
        _repositoryDbContext = repositoryDbContext;
        _repositoryMapper = repositoryMapper;
    }

    public async Task<IReadOnlyList<Template>> GetAllForAdminAsync()
    {
        var entities = await _repositoryDbContext.Templates
            .AsNoTracking()
            .OrderBy(x => x.LanguageCode)
            .ThenBy(x => x.SenderIdentityId)
            .ToListAsync();

        return _repositoryMapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<Template?> GetForAdminAsync(Guid id)
    {
        var entity = await _repositoryDbContext.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return _repositoryMapper.Map(entity);
    }

    public async Task<Template?> GetActiveAsync(Guid senderIdentityId, string languageCode)
    {
        var entity = await _repositoryDbContext.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SenderIdentityId == senderIdentityId &&
                x.LanguageCode == languageCode &&
                x.IsActive);

        return _repositoryMapper.Map(entity);
    }
}
