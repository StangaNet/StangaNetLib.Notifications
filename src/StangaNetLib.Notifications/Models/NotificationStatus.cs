namespace StangaNetLib.Notifications.Models;

/// <summary>
/// Lifecycle status of a dispatched notification.
/// </summary>
public enum NotificationStatus
{
    /// <summary>Created and persisted, not yet sent.</summary>
    Pending,

    /// <summary>Accepted by the channel provider. Delivery is not yet confirmed.</summary>
    Sent,

    /// <summary>Confirmed delivered to the recipient (provider-reported, if supported).</summary>
    Delivered,

    /// <summary>All send attempts exhausted without success.</summary>
    Failed,
}
