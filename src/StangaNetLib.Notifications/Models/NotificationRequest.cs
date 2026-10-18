namespace StangaNetLib.Notifications.Models;

/// <summary>
/// Describes a notification to be dispatched by <see cref="Abstractions.INotificationSender"/>.
/// Supply either <see cref="TemplateId"/> (for template-based rendering) or <see cref="Body"/> (for raw content).
/// </summary>
public sealed class NotificationRequest
{
    /// <summary>Logical identifier of the recipient (e.g. user ID). Used for delivery tracking.</summary>
    public string RecipientId { get; init; } = string.Empty;

    /// <summary>
    /// Channel-specific recipient address: email address for <see cref="NotificationChannel.Email"/>,
    /// device token for <see cref="NotificationChannel.Push"/>, user ID for <see cref="NotificationChannel.InApp"/>.
    /// </summary>
    public string RecipientAddress { get; init; } = string.Empty;

    /// <summary>Delivery channel for this notification.</summary>
    public NotificationChannel Channel { get; init; }

    /// <summary>Template identifier. When set, the template engine resolves and renders the body. Mutually exclusive with <see cref="Body"/>.</summary>
    public string? TemplateId { get; init; }

    /// <summary>Raw subject line. Ignored when <see cref="TemplateId"/> is set (subject comes from the template).</summary>
    public string? Subject { get; init; }

    /// <summary>Raw notification body. Required when <see cref="TemplateId"/> is <c>null</c>.</summary>
    public string? Body { get; init; }

    /// <summary>Key/value pairs substituted into template placeholders (e.g. <c>{{UserName}}</c>). Ignored when <see cref="TemplateId"/> is <c>null</c>.</summary>
    public IReadOnlyDictionary<string, string> Data { get; init; } = new Dictionary<string, string>();

    /// <summary>Optional correlation ID for tracing the notification across systems.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Optional UTC expiry time. If set and the current time exceeds this value, the dispatcher
    /// returns <c>Notifications.Expired</c> without contacting any provider.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}
