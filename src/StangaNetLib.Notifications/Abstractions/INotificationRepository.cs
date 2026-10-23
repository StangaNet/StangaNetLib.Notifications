using StangaNetLib.Core.Common;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Abstractions;

/// <summary>
/// Persists and queries <see cref="NotificationRecord"/> instances for delivery tracking.
/// Provide a project-specific implementation (e.g. EF Core) and register it in place of the default in-memory store.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Persists a new notification record. The record ID must be unique.
    /// </summary>
    /// <param name="record">The record to persist.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns><see cref="Result.Success()"/> on success, or a failure describing a persistence error.</returns>
    Task<Result> SaveAsync(NotificationRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overwrites the stored record with the provided instance (matched by <see cref="NotificationRecord.Id"/>).
    /// </summary>
    /// <param name="record">The updated record.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns><see cref="Result.Success()"/> on success, or a failure describing a persistence error.</returns>
    Task<Result> UpdateAsync(NotificationRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification record by its unique ID.
    /// </summary>
    /// <param name="id">The record ID.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the record,
    /// or <c>NotificationRecord.NotFound</c> if no record exists for <paramref name="id"/>.
    /// </returns>
    Task<Result<NotificationRecord>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns notification records for a given recipient, ordered by creation time (oldest first).
    /// Returns an empty list when no records exist.
    /// </summary>
    /// <param name="recipientId">The logical recipient identifier.</param>
    /// <param name="skip">Number of records to skip (for pagination). Defaults to 0.</param>
    /// <param name="take">Maximum number of records to return. Defaults to <see cref="int.MaxValue"/> (all).</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>A successful <see cref="Result{T}"/> with the matching records (never <c>null</c>).</returns>
    Task<Result<IReadOnlyList<NotificationRecord>>> GetByRecipientAsync(
        string recipientId,
        int skip = 0,
        int take = int.MaxValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns in-app inbox items for <paramref name="recipientId"/> (newest first).
    /// </summary>
    Task<Result<IReadOnlyList<NotificationRecord>>> GetInboxAsync(
        string recipientId,
        int skip = 0,
        int take = 50,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>Counts unread in-app notifications for <paramref name="recipientId"/>.</summary>
    Task<Result<int>> GetUnreadCountAsync(string recipientId, CancellationToken cancellationToken = default);

    /// <summary>Marks all in-app notifications for <paramref name="recipientId"/> as read.</summary>
    Task<Result> MarkAllReadAsync(string recipientId, CancellationToken cancellationToken = default);
}
