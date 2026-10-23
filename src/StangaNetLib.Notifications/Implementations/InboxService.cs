using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Notifications.Abstractions;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Implementations;

/// <summary>Default <see cref="IInboxService"/> backed by <see cref="INotificationRepository"/>.</summary>
internal sealed class InboxService : IInboxService
{
    private readonly INotificationRepository _repository;

    public InboxService(INotificationRepository repository)
    {
        _repository = Guard.Against.Null(repository, nameof(repository));
    }

    public Task<Result<IReadOnlyList<NotificationRecord>>> GetInboxAsync(
        string recipientId,
        int skip = 0,
        int take = 50,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(recipientId, nameof(recipientId));
        return _repository.GetInboxAsync(recipientId, skip, take, unreadOnly, cancellationToken);
    }

    public Task<Result<int>> GetUnreadCountAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(recipientId, nameof(recipientId));
        return _repository.GetUnreadCountAsync(recipientId, cancellationToken);
    }

    public async Task<Result> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(notificationId, nameof(notificationId));
        var result = await _repository.GetByIdAsync(notificationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        var record = result.Value!;
        record.IsRead = true;
        record.ReadAt = DateTimeOffset.UtcNow;
        record.UpdatedAt = record.ReadAt;
        return await _repository.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> MarkAsUnreadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(notificationId, nameof(notificationId));
        var result = await _repository.GetByIdAsync(notificationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        var record = result.Value!;
        record.IsRead = false;
        record.ReadAt = null;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        return await _repository.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
    }

    public Task<Result> MarkAllReadAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(recipientId, nameof(recipientId));
        return _repository.MarkAllReadAsync(recipientId, cancellationToken);
    }
}
