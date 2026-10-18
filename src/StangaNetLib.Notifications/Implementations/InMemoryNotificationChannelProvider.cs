using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Notifications.Abstractions;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Implementations;

/// <summary>
/// An in-memory <see cref="INotificationChannelProvider"/> that records sent notifications without performing real delivery.
/// Suitable for testing and development environments.
/// </summary>
public sealed class InMemoryNotificationChannelProvider : INotificationChannelProvider
{
    private readonly List<NotificationRecord> _sent = [];
    private readonly object _lock = new();

    /// <summary>Initializes the provider for the specified channel.</summary>
    /// <param name="channel">The channel this provider handles. Defaults to <see cref="NotificationChannel.InApp"/>.</param>
    public InMemoryNotificationChannelProvider(NotificationChannel channel = NotificationChannel.InApp)
    {
        Channel = channel;
    }

    /// <inheritdoc />
    public NotificationChannel Channel { get; }

    /// <summary>All notifications accepted by this provider in the order they were received.</summary>
    public IReadOnlyList<NotificationRecord> SentNotifications
    {
        get
        {
            lock (_lock)
                return _sent.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public Task<Result> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(record, nameof(record));
        lock (_lock)
            _sent.Add(record);
        return Task.FromResult(Result.Success());
    }
}
