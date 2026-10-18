namespace StangaNetLib.Notifications.Models;

/// <summary>
/// A named template registered with <see cref="Abstractions.INotificationTemplateEngine"/>.
/// Placeholders use double-brace syntax: <c>{{Key}}</c>.
/// </summary>
public sealed class NotificationTemplate
{
    /// <summary>Unique identifier used to look up this template. Case-sensitive.</summary>
    public string TemplateId { get; init; } = string.Empty;

    /// <summary>The channel this template applies to. Templates are scoped per channel.</summary>
    public NotificationChannel Channel { get; init; }

    /// <summary>Optional subject template (e.g. for email). May contain <c>{{Key}}</c> placeholders.</summary>
    public string? SubjectTemplate { get; init; }

    /// <summary>Notification body template. May contain <c>{{Key}}</c> placeholders. Must not be empty.</summary>
    public string BodyTemplate { get; init; } = string.Empty;
}
