using System.Collections.Concurrent;
using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Notifications.Abstractions;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Implementations;

/// <summary>
/// Thread-safe <see cref="INotificationRepository"/> backed by a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// Intended for testing and development; records are not persisted across restarts.
/// </summary>
internal sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<string, NotificationRecord> _store = new(StringComparer.Ordinal);

    public Task<Result> SaveAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(record, nameof(record));
        _store[record.Id] = record;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UpdateAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(record, nameof(record));
        _store[record.Id] = record;
        return Task.FromResult(Result.Success());
    }

    public Task<Result<NotificationRecord>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(id, nameof(id));
        return _store.TryGetValue(id, out var record)
            ? Task.FromResult(Result<NotificationRecord>.Success(record))
            : Task.FromResult(Result<NotificationRecord>.Failure(Error.NotFound("NotificationRecord", id)));
    }

    public Task<Result<IReadOnlyList<NotificationRecord>>> GetByRecipientAsync(
        string recipientId,
        int skip = 0,
        int take = int.MaxValue,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(recipientId, nameof(recipientId));
        IReadOnlyList<NotificationRecord> records = _store.Values
            .Where(r => r.RecipientId == recipientId)
            .OrderBy(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList()
            .AsReadOnly();
        return Task.FromResult(Result<IReadOnlyList<NotificationRecord>>.Success(records));
    }
}
