using StangaNetLib.Core.Common;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Abstractions;

/// <summary>
/// Handles the actual delivery of a notification to a specific channel.
/// Implement this interface for each supported channel (email, push, in-app)
/// and register each implementation with the DI container.
/// </summary>
/// <example>
/// <code>
/// public sealed class SmtpEmailProvider : INotificationChannelProvider
/// {
///     public NotificationChannel Channel => NotificationChannel.Email;
///
///     public async Task&lt;Result&gt; SendAsync(NotificationRecord record, CancellationToken ct)
///     {
///         // send via SMTP
///         return Result.Success();
///     }
/// }
/// </code>
/// </example>
public interface INotificationChannelProvider
{
    /// <summary>The channel this provider handles.</summary>
    NotificationChannel Channel { get; }

    /// <summary>
    /// Sends the notification represented by <paramref name="record"/> to its recipient.
    /// Return <see cref="Result.Failure(StangaNetLib.Core.Common.Error)"/> to signal a retriable error;
    /// the dispatcher will retry according to the configured <c>MaxRetryCount</c>.
    /// </summary>
    /// <param name="record">The fully rendered notification record to deliver.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns><see cref="Result.Success()"/> on delivery acceptance, or a failure describing the error.</returns>
    Task<Result> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default);
}
