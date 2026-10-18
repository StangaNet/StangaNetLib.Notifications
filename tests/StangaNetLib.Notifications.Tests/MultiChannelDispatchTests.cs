using StangaNetLib.Core.Common;

namespace StangaNetLib.Notifications.Tests;

public sealed class MultiChannelDispatchTests
{
    private readonly InMemoryNotificationChannelProvider _emailProvider = new(NotificationChannel.Email);
    private readonly InMemoryNotificationChannelProvider _pushProvider  = new(NotificationChannel.Push);
    private readonly InMemoryNotificationChannelProvider _inAppProvider = new(NotificationChannel.InApp);
    private readonly InMemoryNotificationRepository _repository = new();
    private readonly SimpleTemplateEngine _templateEngine = new();

    private NotificationDispatcher CreateSender()
    {
        var opts = Options.Create(new NotificationsSettings { MaxRetryCount = 0, RetryDelay = TimeSpan.Zero });
        return new NotificationDispatcher(
            [_emailProvider, _pushProvider, _inAppProvider],
            _templateEngine, _repository, opts);
    }

    // ── Provider routing ─────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_EmailChannel_RoutesOnlyToEmailProvider()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sender = CreateSender();

        await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u1", RecipientAddress = "u@example.com",
            Channel = NotificationChannel.Email, Body = "email body",
        }, ct);

        _emailProvider.SentNotifications.Should().HaveCount(1);
        _pushProvider.SentNotifications.Should().BeEmpty();
        _inAppProvider.SentNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_PushChannel_RoutesOnlyToPushProvider()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sender = CreateSender();

        await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u2", RecipientAddress = "device-token",
            Channel = NotificationChannel.Push, Body = "push body",
        }, ct);

        _pushProvider.SentNotifications.Should().HaveCount(1);
        _emailProvider.SentNotifications.Should().BeEmpty();
        _inAppProvider.SentNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_InAppChannel_RoutesOnlyToInAppProvider()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sender = CreateSender();

        await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u3", RecipientAddress = "u3",
            Channel = NotificationChannel.InApp, Body = "in-app body",
        }, ct);

        _inAppProvider.SentNotifications.Should().HaveCount(1);
        _emailProvider.SentNotifications.Should().BeEmpty();
        _pushProvider.SentNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_TwoDifferentChannels_EachProviderReceivesOne()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sender = CreateSender();

        await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u4", RecipientAddress = "u@example.com",
            Channel = NotificationChannel.Email, Body = "email",
        }, ct);
        await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u4", RecipientAddress = "device-token",
            Channel = NotificationChannel.Push, Body = "push",
        }, ct);

        _emailProvider.SentNotifications.Should().HaveCount(1);
        _pushProvider.SentNotifications.Should().HaveCount(1);
    }

    // ── CorrelationId propagation ─────────────────────────────────────────

    [Fact]
    public async Task SendAsync_WithCorrelationId_RecordPreservesCorrelationId()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sender = CreateSender();

        var sendResult = await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u5", RecipientAddress = "u@example.com",
            Channel = NotificationChannel.Email, Body = "correlated",
            CorrelationId = "trace-abc-123",
        }, ct);

        sendResult.IsSuccess.Should().BeTrue();
        var statusResult = await sender.GetStatusAsync(sendResult.Value!, ct);
        statusResult.Value!.CorrelationId.Should().Be("trace-abc-123");
    }

    [Fact]
    public async Task SendAsync_WithoutCorrelationId_RecordCorrelationIdIsNull()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sender = CreateSender();

        var sendResult = await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u6", RecipientAddress = "u@example.com",
            Channel = NotificationChannel.Email, Body = "no-correlation",
        }, ct);

        var statusResult = await sender.GetStatusAsync(sendResult.Value!, ct);
        statusResult.Value!.CorrelationId.Should().BeNull();
    }

    // ── Validation: RecipientAddress ──────────────────────────────────────

    [Fact]
    public async Task SendAsync_EmptyRecipientAddress_ReturnsValidationFailure()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sender = CreateSender();

        var result = await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u7", RecipientAddress = "",
            Channel = NotificationChannel.Email, Body = "body",
        }, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Contain("RecipientAddress");
    }

    [Fact]
    public async Task SendAsync_WhitespaceRecipientAddress_ReturnsValidationFailure()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sender = CreateSender();

        var result = await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u8", RecipientAddress = "   ",
            Channel = NotificationChannel.Email, Body = "body",
        }, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    // ── AttemptCount tracking ─────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_SuccessfulSend_RecordAttemptCountIsOne()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sender = CreateSender();

        var sendResult = await sender.SendAsync(new NotificationRequest
        {
            RecipientId = "u9", RecipientAddress = "u@example.com",
            Channel = NotificationChannel.Email, Body = "attempt-test",
        }, ct);

        var statusResult = await sender.GetStatusAsync(sendResult.Value!, ct);
        statusResult.Value!.AttemptCount.Should().Be(1);
    }
}
