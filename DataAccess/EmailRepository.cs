using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class EmailRepository : BaseRepository<Email, EmailEntity, EmailEntityMapper>, IEmailRepository
{
    private readonly AppDbContext _repositoryDbContext;
    private readonly EmailEntityMapper _repositoryMapper;

    public EmailRepository(AppDbContext repositoryDbContext, EmailEntityMapper repositoryMapper) 
        : base(repositoryDbContext, repositoryMapper)
    {
        _repositoryDbContext = repositoryDbContext;
        _repositoryMapper = repositoryMapper;
    }

    public async Task<IReadOnlyList<Email>> GetAllForAdminAsync()
    {
        var entities = await _repositoryDbContext.Emails
            .AsNoTracking()
            .OrderByDescending(x => x.SentAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return _repositoryMapper.Map(entities)?.ToList() ?? [];
    }

    public async Task<Email?> GetForAdminAsync(Guid id)
    {
        var entity = await _repositoryDbContext.Emails
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return _repositoryMapper.Map(entity);
    }

    public Task<int> UpdateDeliveryStatusAsync(
        Guid id,
        EEmailStatus status,
        DateTime? sentAt,
        CancellationToken cancellationToken = default)
    {
        return _repositoryDbContext.Emails
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, status)
                    .SetProperty(x => x.SentAt, sentAt),
                cancellationToken);
    }
}
