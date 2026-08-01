using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IEmailRepository : IBaseRepository<Email>
{
    Task<IReadOnlyList<Email>> GetAllForAdminAsync();
    Task<Email?> GetForAdminAsync(Guid id);

    /// <summary>Looks up an email by its originating envelope id (idempotency key). Null if unseen.</summary>
    Task<Email?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts and commits a pending email keyed on its (unique) <c>MessageId</c>. Returns the created
    /// row, or <c>null</c> when a row with that <c>MessageId</c> already exists (a concurrent duplicate
    /// delivery) — the caller treats null as "already handled, ack silently". This is the idempotency
    /// insert-before-send: it commits before any mail is sent.
    /// </summary>
    Task<Email?> TryCreatePendingAsync(Email email, CancellationToken cancellationToken = default);

    Task<int> UpdateDeliveryStatusAsync(
        Guid id,
        EEmailStatus status,
        string? subject,
        DateTime? sentAt,
        CancellationToken cancellationToken = default);
}
