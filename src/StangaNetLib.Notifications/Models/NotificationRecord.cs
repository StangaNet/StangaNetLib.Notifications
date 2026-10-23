namespace StangaNetLib.Notifications.Models;

/// <summary>
/// Persisted record of a dispatched notification, including delivery status and attempt tracking.
/// Consumed by <see cref="Abstractions.INotificationRepository"/> and <see cref="Abstractions.INotificationChannelProvider"/>.
/// </summary>
public sealed class NotificationRecord
{
    /// <summary>Unique identifier for this notification record.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Logical recipient identifier (mirrors <see cref="NotificationRequest.RecipientId"/>).</summary>
    public string RecipientId { get; init; } = string.Empty;

    /// <summary>Channel-specific address (email / device token / user ID).</summary>
    public string RecipientAddress { get; init; } = string.Empty;

    /// <summary>Delivery channel for this record.</summary>
    public NotificationChannel Channel { get; init; }

    /// <summary>Current lifecycle status.</summary>
    public NotificationStatus Status { get; set; }

    /// <summary>Rendered subject line, or <c>null</c> for channels that have no subject.</summary>
    public string? Subject { get; init; }

    /// <summary>Rendered notification body.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Total number of send attempts made so far.</summary>
    public int AttemptCount { get; set; }

    /// <summary>UTC timestamp when the record was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>UTC timestamp when the notification was successfully accepted by the provider.</summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>Human-readable reason for the last failure, or <c>null</c> on success.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Optional correlation ID propagated from <see cref="NotificationRequest.CorrelationId"/>.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>UTC timestamp of the last status change (set when <see cref="Status"/> is updated).</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Whether the recipient has marked this in-app notification as read.
    /// Meaningful for <see cref="NotificationChannel.InApp"/>; ignored by other channels.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>UTC timestamp when the notification was marked read, or <c>null</c> if unread.</summary>
    public DateTimeOffset? ReadAt { get; set; }
}
