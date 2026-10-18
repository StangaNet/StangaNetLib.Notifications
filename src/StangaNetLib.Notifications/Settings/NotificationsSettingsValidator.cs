using StangaNetLib.Core.Validators;

namespace StangaNetLib.Notifications.Settings;

/// <summary>
/// Validates <see cref="NotificationsSettings"/> at startup. Rejects negative delays,
/// <see cref="NotificationsSettings.MaxRetryCount"/> above the hard cap, and a
/// <see cref="NotificationsSettings.MaxRetryDelay"/> smaller than <see cref="NotificationsSettings.RetryDelay"/>
/// when exponential backoff is enabled.
/// </summary>
internal sealed class NotificationsSettingsValidator : SettingsValidatorBase<NotificationsSettings>
{
    private const int MaxAllowedRetryCount = 20;

    protected override void ValidateCore(NotificationsSettings options, List<string> errors)
    {
        if (options.MaxRetryCount < 0)
            errors.Add($"{nameof(options.MaxRetryCount)} must be non-negative.");

        if (options.MaxRetryCount > MaxAllowedRetryCount)
            errors.Add($"{nameof(options.MaxRetryCount)} must not exceed {MaxAllowedRetryCount}.");

        if (options.RetryDelay < TimeSpan.Zero)
            errors.Add($"{nameof(options.RetryDelay)} must be non-negative.");

        if (options.MaxRetryDelay < TimeSpan.Zero)
            errors.Add($"{nameof(options.MaxRetryDelay)} must be non-negative.");

        if (options.UseExponentialBackoff && options.MaxRetryDelay < options.RetryDelay)
            errors.Add($"{nameof(options.MaxRetryDelay)} must be greater than or equal to {nameof(options.RetryDelay)} when exponential backoff is enabled.");
    }
}
