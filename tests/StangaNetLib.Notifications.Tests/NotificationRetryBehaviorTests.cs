namespace StangaNetLib.Notifications.Tests;

public sealed class NotificationRetryBehaviorTests
{
    private readonly InMemoryNotificationRepository _repository = new();
    private readonly SimpleTemplateEngine _engine = new();

    private NotificationDispatcher CreateSender(NotificationsSettings settings)
        => new(
            [new CountingProvider(NotificationChannel.Email, succeedOnAttempt: int.MaxValue)],
            _engine, _repository, Options.Create(settings));

    private NotificationDispatcher CreateSenderWithProvider(INotificationChannelProvider provider, NotificationsSettings settings)
        => new([provider], _engine, _repository, Options.Create(settings));

    private static NotificationRequest EmailRequest(string id = "u") => new()
    {
        RecipientId = id, RecipientAddress = "u@example.com",
        Channel = NotificationChannel.Email, Body = "test",
    };

    // ── MaxRetryCount = 0 ─────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_MaxRetryCountZero_ExactlyOneAttemptMade()
    {
        var ct = TestContext.Current.CancellationToken;
        var provider = new CountingProvider(NotificationChannel.Email, succeedOnAttempt: int.MaxValue);
        var sender   = CreateSenderWithProvider(provider, new NotificationsSettings { MaxRetryCount = 0, RetryDelay = TimeSpan.Zero });

        await sender.SendAsync(EmailRequest(), ct);

        provider.AttemptCount.Should().Be(1);
    }

    // ── Fixed retry delay ─────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_MaxRetryCountTwo_ThreeAttemptsTotal()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new CountingProvider(NotificationChannel.Email, succeedOnAttempt: int.MaxValue);
        var sender   = CreateSenderWithProvider(provider, new NotificationsSettings
        {
            MaxRetryCount = 2, RetryDelay = TimeSpan.Zero, UseExponentialBackoff = false,
        });

        await sender.SendAsync(EmailRequest(), ct);

        provider.AttemptCount.Should().Be(3); // 1 initial + 2 retries
    }

    [Fact]
    public async Task SendAsync_SucceedsOnSecondAttempt_ReturnsTwoAttempts()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new CountingProvider(NotificationChannel.Email, succeedOnAttempt: 2);
        var sender   = CreateSenderWithProvider(provider, new NotificationsSettings
        {
            MaxRetryCount = 3, RetryDelay = TimeSpan.Zero, UseExponentialBackoff = false,
        });

        var result = await sender.SendAsync(EmailRequest(), ct);

        result.IsSuccess.Should().BeTrue();
        provider.AttemptCount.Should().Be(2);
    }

    // ── Failed record state ───────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_AllAttemptsExhausted_RecordStatusIsFailed()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new CountingProvider(NotificationChannel.Email, succeedOnAttempt: int.MaxValue);
        var sender   = CreateSenderWithProvider(provider, new NotificationsSettings
        {
            MaxRetryCount = 1, RetryDelay = TimeSpan.Zero,
        });

        var result = await sender.SendAsync(EmailRequest("fail-user"), ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notifications.SendFailed");

        var records = await _repository.GetByRecipientAsync("fail-user", cancellationToken: ct);
        records.Value!.Should().ContainSingle(r => r.Status == NotificationStatus.Failed);
    }

    [Fact]
    public async Task SendAsync_AllAttemptsExhausted_RecordHasFailureReason()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new CountingProvider(NotificationChannel.Email, succeedOnAttempt: int.MaxValue);
        var sender   = CreateSenderWithProvider(provider, new NotificationsSettings
        {
            MaxRetryCount = 0, RetryDelay = TimeSpan.Zero,
        });

        var result = await sender.SendAsync(EmailRequest("fail-reason"), ct);

        result.IsFailure.Should().BeTrue();
        var records = await _repository.GetByRecipientAsync("fail-reason", cancellationToken: ct);
        records.Value![0].FailureReason.Should().NotBeNullOrEmpty();
    }

    // ── Exponential backoff compute (via AttemptCount) ────────────────────

    [Fact]
    public async Task SendAsync_ExponentialBackoff_RecordAttemptCountMatchesRetries()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new CountingProvider(NotificationChannel.Email, succeedOnAttempt: 3);
        var sender   = CreateSenderWithProvider(provider, new NotificationsSettings
        {
            MaxRetryCount = 5, RetryDelay = TimeSpan.Zero, UseExponentialBackoff = true,
        });

        var result = await sender.SendAsync(EmailRequest("exp-user"), ct);

        result.IsSuccess.Should().BeTrue();
        provider.AttemptCount.Should().Be(3);
    }

    // ── AttemptCount incremented in record ───────────────────────────────

    [Fact]
    public async Task SendAsync_TwoRetriesBeforeSuccess_RecordAttemptCountIsThree()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new CountingProvider(NotificationChannel.Email, succeedOnAttempt: 3);
        var sender   = CreateSenderWithProvider(provider, new NotificationsSettings
        {
            MaxRetryCount = 5, RetryDelay = TimeSpan.Zero,
        });

        var sendResult = await sender.SendAsync(EmailRequest("attempt-count"), ct);

        sendResult.IsSuccess.Should().BeTrue();
        var statusResult = await sender.GetStatusAsync(sendResult.Value!, ct);
        statusResult.Value!.AttemptCount.Should().Be(3);
    }
}

file sealed class CountingProvider : INotificationChannelProvider
{
    private readonly int _succeedOnAttempt;

    public CountingProvider(NotificationChannel channel, int succeedOnAttempt)
    {
        Channel = channel;
        _succeedOnAttempt = succeedOnAttempt;
    }

    public NotificationChannel Channel { get; }
    public int AttemptCount { get; private set; }

    public Task<StangaNetLib.Core.Common.Result> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        AttemptCount++;
        return AttemptCount >= _succeedOnAttempt
            ? Task.FromResult(StangaNetLib.Core.Common.Result.Success())
            : Task.FromResult(StangaNetLib.Core.Common.Result.Failure(
                StangaNetLib.Core.Common.Error.Internal($"Transient failure on attempt {AttemptCount}.")));
    }
}
