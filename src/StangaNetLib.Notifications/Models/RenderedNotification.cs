namespace StangaNetLib.Notifications.Models;

/// <summary>
/// The result of rendering a <see cref="NotificationTemplate"/> with a set of data values.
/// </summary>
/// <param name="Subject">Rendered subject line, or <c>null</c> when the template has no subject.</param>
/// <param name="Body">Rendered notification body.</param>
public sealed record RenderedNotification(string? Subject, string Body);
