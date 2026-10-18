namespace StangaNetLib.Notifications.Models;

/// <summary>
/// Identifies the delivery channel for a notification.
/// </summary>
public enum NotificationChannel
{
    /// <summary>Sent via email to a recipient address.</summary>
    Email,

    /// <summary>Delivered in-app as a user-facing alert or feed entry.</summary>
    InApp,

    /// <summary>Sent as a push notification to a device token.</summary>
    Push,
}
