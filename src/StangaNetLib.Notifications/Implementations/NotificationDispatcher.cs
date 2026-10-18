using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Notifications.Abstractions;
using StangaNetLib.Notifications.Models;
using StangaNetLib.Notifications.Settings;
using StangaNetLib.Resilience.Execution;

namespace StangaNetLib.Notifications.Implementations;

/// <summary>
/// Default <see cref="INotificationSender"/> implementation. Validates requests, resolves and renders templates,
/// persists <see cref="NotificationRecord"/> instances, routes delivery to the matching <see cref="INotificationChannelProvider"/>,
/// and retries on transient failure.
/// </summary>
/// <remarks>
/// When an <see cref="IResilientExecutor"/> is registered in the DI container it is injected automatically
/// and used for channel delivery (retry, circuit-breaker, and timeout are driven by the
/// <c>"Notifications"</c> pipeline in <c>ResilienceSettings.Pipelines</c>).
/// When no executor is present the library falls back to the built-in retry loop governed by
/// <see cref="NotificationsSettings.MaxRetryCount"/>, <see cref="NotificationsSettings.RetryDelay"/>, and
/// <see cref="NotificationsSettings.UseExponentialBackoff"/>.
/// </remarks>
internal sealed class NotificationDispatcher : INotificationSender
{
    /// <summary>Resilience pipeline name consumed from <c>ResilienceSettings.Pipelines</c> when <see cref="IResilientExecutor"/> is active.</summary>
    internal const string PipelineName = "Notifications";

    private readonly IReadOnlyDictionary<NotificationChannel, INotificationChannelProvider> _providers;
    private readonly INotificationTemplateEngine _templateEngine;
    private readonly INotificationRepository _repository;
    private readonly NotificationsSettings _settings;
    private readonly IResilientExecutor? _resilience;

    public NotificationDispatcher(
        IEnumerable<INotificationChannelProvider> providers,
        INotificationTemplateEngine templateEngine,
        INotificationRepository repository,
        IOptions<NotificationsSettings> settings,
        IResilientExecutor? resilience = null)
    {
        Guard.Against.Null(providers, nameof(providers));
        _templateEngine = Guard.Against.Null(templateEngine, nameof(templateEngine));
        _repository = Guard.Against.Null(repository, nameof(repository));
        _settings = Guard.Against.Null(settings, nameof(settings)).Value;
        _resilience = resilience;
        _providers = providers.ToDictionary(p => p.Channel);
    }

    public async Task<Result<string>> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(request, nameof(request));

        if (string.IsNullOrWhiteSpace(request.RecipientId))
            return Result<string>.Failure(Error.Validation("RecipientId", "RecipientId must not be empty."));
        if (string.IsNullOrWhiteSpace(request.RecipientAddress))
            return Result<string>.Failure(Error.Validation("RecipientAddress", "RecipientAddress must not be empty."));

        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            return Result<string>.Failure(new Error(
                "Notifications.Expired",
                $"The notification expired at {request.ExpiresAt.Value:O} and was not sent.",
                ErrorType.Validation));

        string? subject = request.Subject;
        string? body = request.Body;

        if (request.TemplateId is not null)
        {
            var resolveResult = _templateEngine.Resolve(request.TemplateId, request.Channel);
            if (resolveResult.IsFailure)
                return Result<string>.Failure(resolveResult.Error);

            var renderResult = _templateEngine.Render(resolveResult.Value!, request.Data);
            if (renderResult.IsFailure)
                return Result<string>.Failure(renderResult.Error);

            subject = renderResult.Value!.Subject;
            body = renderResult.Value!.Body;
        }

        if (string.IsNullOrWhiteSpace(body))
            return Result<string>.Failure(Error.Validation("Body", "Notification body must not be empty. Provide a Body or a TemplateId."));

        if (!_providers.TryGetValue(request.Channel, out var provider))
            return Result<string>.Failure(new Error(
                "Notifications.NoProvider",
                $"No provider is registered for channel '{request.Channel}'.",
                ErrorType.Internal));

        var record = new NotificationRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            RecipientId = request.RecipientId,
            RecipientAddress = request.RecipientAddress,
            Channel = request.Channel,
            Status = NotificationStatus.Pending,
            Subject = subject,
            Body = body!,
            AttemptCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            CorrelationId = request.CorrelationId,
        };

        var saveResult = await _repository.SaveAsync(record, cancellationToken);
        if (saveResult.IsFailure)
            return Result<string>.Failure(saveResult.Error);

        var sendResult = await TrySendAsync(provider, record, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (sendResult.IsFailure)
        {
            record.Status = NotificationStatus.Failed;
            record.FailureReason = sendResult.Error.Description;
        }
        else
        {
            record.Status = NotificationStatus.Sent;
            record.SentAt = now;
        }
        record.UpdatedAt = now;

        await _repository.UpdateAsync(record, cancellationToken);

        return sendResult.IsFailure
            ? Result<string>.Failure(new Error("Notifications.SendFailed", sendResult.Error.Description, ErrorType.Internal))
            : Result<string>.Success(record.Id);
    }

    public async Task<Result<NotificationRecord>> GetStatusAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(notificationId, nameof(notificationId));
        return await _repository.GetByIdAsync(notificationId, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<NotificationRecord>>> GetByRecipientAsync(
        string recipientId,
        int skip = 0,
        int take = int.MaxValue,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(recipientId, nameof(recipientId));
        return await _repository.GetByRecipientAsync(recipientId, skip, take, cancellationToken);
    }

    private Task<Result> TrySendAsync(
        INotificationChannelProvider provider,
        NotificationRecord record,
        CancellationToken cancellationToken)
        => _resilience is not null
            ? TrySendWithResilienceAsync(provider, record, cancellationToken)
            : TrySendWithManualRetryAsync(provider, record, cancellationToken);

    private async Task<Result> TrySendWithResilienceAsync(
        INotificationChannelProvider provider,
        NotificationRecord record,
        CancellationToken cancellationToken)
    {
        return await _resilience!.ExecuteAsync(PipelineName, async innerCt =>
        {
            record.AttemptCount++;
            var sendResult = await provider.SendAsync(record, innerCt);
            if (sendResult.IsFailure)
                throw new Exception(sendResult.Error.Description);
        }, cancellationToken);
    }

    private async Task<Result> TrySendWithManualRetryAsync(
        INotificationChannelProvider provider,
        NotificationRecord record,
        CancellationToken cancellationToken)
    {
        Result lastResult = Result.Failure("Not attempted.");
        for (int attempt = 0; attempt <= _settings.MaxRetryCount; attempt++)
        {
            if (attempt > 0)
            {
                var delay = ComputeDelay(attempt);
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken);
            }

            record.AttemptCount++;
            lastResult = await provider.SendAsync(record, cancellationToken);
            if (lastResult.IsSuccess)
                return lastResult;
        }
        return lastResult;
    }

    private TimeSpan ComputeDelay(int attempt)
    {
        if (_settings.RetryDelay <= TimeSpan.Zero)
            return TimeSpan.Zero;

        if (!_settings.UseExponentialBackoff)
            return _settings.RetryDelay;

        var ticks = _settings.RetryDelay.Ticks * (long)Math.Pow(2, attempt - 1);
        var delay = TimeSpan.FromTicks(Math.Min(ticks, _settings.MaxRetryDelay.Ticks));
        return delay;
    }
}
