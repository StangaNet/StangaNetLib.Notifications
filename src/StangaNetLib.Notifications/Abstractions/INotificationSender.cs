using StangaNetLib.Core.Common;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Abstractions;

/// <summary>
/// Dispatches notifications across registered channel providers with template rendering, retry, and delivery tracking.
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Dispatches a notification using the specified channel and content.
    /// Validates the request, renders the template (if <see cref="NotificationRequest.TemplateId"/> is set),
    /// persists a <see cref="NotificationRecord"/>, routes to the matching <see cref="INotificationChannelProvider"/>,
    /// and retries on failure according to <c>NotificationsSettings.MaxRetryCount</c>.
    /// </summary>
    /// <param name="request">The notification to dispatch.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the notification record ID on success,
    /// or a failure with one of the following error codes:
    /// <list type="bullet">
    ///   <item><c>Validation.RecipientId</c> — RecipientId is empty.</item>
    ///   <item><c>Validation.RecipientAddress</c> — RecipientAddress is empty.</item>
    ///   <item><c>Validation.Body</c> — no body and no template provided.</item>
    ///   <item><c>NotificationTemplate.NotFound</c> — templateId not registered for the channel.</item>
    ///   <item><c>Notifications.Expired</c> — <see cref="NotificationRequest.ExpiresAt"/> is in the past.</item>
    ///   <item><c>Notifications.NoProvider</c> — no provider registered for the channel.</item>
    ///   <item><c>Notifications.SendFailed</c> — all retry attempts exhausted.</item>
    /// </list>
    /// </returns>
    Task<Result<string>> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current delivery status of a previously dispatched notification.
    /// </summary>
    /// <param name="notificationId">The record ID returned by <see cref="SendAsync"/>.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the <see cref="NotificationRecord"/>,
    /// or <c>NotificationRecord.NotFound</c> if the ID does not exist.
    /// </returns>
    Task<Result<NotificationRecord>> GetStatusAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns notification records dispatched to the specified recipient, ordered by creation time (oldest first).
    /// Supports pagination via <paramref name="skip"/> and <paramref name="take"/>.
    /// </summary>
    /// <param name="recipientId">The logical recipient identifier.</param>
    /// <param name="skip">Number of records to skip. Defaults to 0.</param>
    /// <param name="take">Maximum number of records to return. Defaults to <see cref="int.MaxValue"/> (all).</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the matching records (never <c>null</c>),
    /// or a failure if <paramref name="recipientId"/> is empty.
    /// </returns>
    Task<Result<IReadOnlyList<NotificationRecord>>> GetByRecipientAsync(
        string recipientId,
        int skip = 0,
        int take = int.MaxValue,
        CancellationToken cancellationToken = default);
}
