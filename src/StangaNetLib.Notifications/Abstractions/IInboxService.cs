using StangaNetLib.Core.Common;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Abstractions;

/// <summary>First-class in-app notification inbox (read / unread).</summary>
public interface IInboxService
{
    /// <summary>
    /// Returns in-app notifications for <paramref name="recipientId"/>, newest first.
    /// </summary>
    Task<Result<IReadOnlyList<NotificationRecord>>> GetInboxAsync(
        string recipientId,
        int skip = 0,
        int take = 50,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the count of unread in-app notifications for <paramref name="recipientId"/>.</summary>
    Task<Result<int>> GetUnreadCountAsync(string recipientId, CancellationToken cancellationToken = default);

    /// <summary>Marks a single notification as read.</summary>
    Task<Result> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>Marks a single notification as unread.</summary>
    Task<Result> MarkAsUnreadAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>Marks all in-app notifications for <paramref name="recipientId"/> as read.</summary>
    Task<Result> MarkAllReadAsync(string recipientId, CancellationToken cancellationToken = default);
}
