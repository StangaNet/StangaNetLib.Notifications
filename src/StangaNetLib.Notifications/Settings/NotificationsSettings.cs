namespace StangaNetLib.Notifications.Settings;

/// <summary>Configuration for StangaNetLib.Notifications services.</summary>
public sealed class NotificationsSettings
{
    /// <summary>The <c>appsettings.json</c> section key. Value: <c>"NotificationsSettings"</c>.</summary>
    public const string SectionName = "NotificationsSettings";

    /// <summary>
    /// Maximum number of retry attempts after the first send failure.
    /// A value of 0 means no retries (one attempt total).
    /// Defaults to 3.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Base delay for the first retry attempt.
    /// When <see cref="UseExponentialBackoff"/> is <c>true</c>, subsequent delays double up to <see cref="MaxRetryDelay"/>.
    /// Defaults to 5 seconds.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Upper cap for exponential backoff delays. Ignored when <see cref="UseExponentialBackoff"/> is <c>false</c>.
    /// Defaults to 60 seconds.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// When <c>true</c>, retry delays follow an exponential backoff strategy: <c>RetryDelay * 2^attempt</c>,
    /// capped at <see cref="MaxRetryDelay"/>. When <c>false</c>, a fixed <see cref="RetryDelay"/> is used.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}
