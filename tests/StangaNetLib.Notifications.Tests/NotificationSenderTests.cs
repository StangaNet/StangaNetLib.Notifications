using StangaNetLib.Core.Common;

namespace StangaNetLib.Notifications.Tests;

public sealed class NotificationSenderTests
{
    private readonly InMemoryNotificationChannelProvider _emailProvider = new(NotificationChannel.Email);
    private readonly InMemoryNotificationRepository _repository = new();
    private readonly SimpleTemplateEngine _templateEngine = new();

    private NotificationDispatcher CreateSender(NotificationsSettings? settings = null)
    {
        var opts = Options.Create(settings ?? new NotificationsSettings { MaxRetryCount = 3, RetryDelay = TimeSpan.Zero });
        return new NotificationDispatcher([_emailProvider], _templateEngine, _repository, opts);
    }

    [Fact]
    public async Task SendAsync_WithRawBody_CreatesAndSendsRecord()
    {
        var sender = CreateSender();
        var request = new NotificationRequest
        {
            RecipientId = "user-1",
            RecipientAddress = "user@example.com",
            Channel = NotificationChannel.Email,
            Subject = "Hello",
            Body = "Hello world",
        };

        var result = await sender.SendAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        _emailProvider.SentNotifications.Should().HaveCount(1);
        _emailProvider.SentNotifications[0].Body.Should().Be("Hello world");
        _emailProvider.SentNotifications[0].RecipientId.Should().Be("user-1");
    }

    [Fact]
    public async Task SendAsync_WithTemplate_RendersAndSends()
    {
        _templateEngine.Register(new NotificationTemplate
        {
            TemplateId = "welcome",
            Channel = NotificationChannel.Email,
            SubjectTemplate = "Welcome, {{UserName}}!",
            BodyTemplate = "Hi {{UserName}}, thanks for joining.",
        });

        var sender = CreateSender();
        var request = new NotificationRequest
        {
            RecipientId = "user-2",
            RecipientAddress = "user2@example.com",
            Channel = NotificationChannel.Email,
            TemplateId = "welcome",
            Data = new Dictionary<string, string> { ["UserName"] = "Alice" },
        };

        var result = await sender.SendAsync(request);

        result.IsSuccess.Should().BeTrue();
        var record = _emailProvider.SentNotifications[0];
        record.Subject.Should().Be("Welcome, Alice!");
        record.Body.Should().Be("Hi Alice, thanks for joining.");
    }

    [Fact]
    public async Task SendAsync_WithUnknownTemplate_ReturnsNotFoundFailure()
    {
        var sender = CreateSender();
        var request = new NotificationRequest
        {
            RecipientId = "user-3",
            RecipientAddress = "user3@example.com",
            Channel = NotificationChannel.Email,
            TemplateId = "missing",
        };

        var result = await sender.SendAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task SendAsync_WithNoProvider_ReturnsNoProviderFailure()
    {
        var opts = Options.Create(new NotificationsSettings
        {
            MaxRetryCount = 0,
            RetryDelay = TimeSpan.Zero,
            EnablePush = true,
        });
        var sender = new NotificationDispatcher([], _templateEngine, _repository, opts);
        var request = new NotificationRequest
        {
            RecipientId = "user-4",
            RecipientAddress = "token-xyz",
            Channel = NotificationChannel.Push,
            Body = "You have a new message.",
        };

        var result = await sender.SendAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notifications.NoProvider");
    }

    [Fact]
    public async Task SendAsync_EmptyRecipientId_ReturnsValidationFailure()
    {
        var sender = CreateSender();
        var request = new NotificationRequest
        {
            RecipientId = "",
            RecipientAddress = "user@example.com",
            Channel = NotificationChannel.Email,
            Body = "Test",
        };

        var result = await sender.SendAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Contain("RecipientId");
    }

    [Fact]
    public async Task SendAsync_EmptyBody_ReturnsValidationFailure()
    {
        var sender = CreateSender();
        var request = new NotificationRequest
        {
            RecipientId = "user-5",
            RecipientAddress = "user@example.com",
            Channel = NotificationChannel.Email,
        };

        var result = await sender.SendAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task SendAsync_ProviderFailsAllAttempts_RecordMarkedFailed()
    {
        var failingProvider = new FailingChannelProvider(NotificationChannel.Email, succeedOnAttempt: int.MaxValue);
        var opts = Options.Create(new NotificationsSettings { MaxRetryCount = 2, RetryDelay = TimeSpan.Zero });
        var sender = new NotificationDispatcher([failingProvider], _templateEngine, _repository, opts);
        var request = new NotificationRequest
        {
            RecipientId = "user-6",
            RecipientAddress = "user@example.com",
            Channel = NotificationChannel.Email,
            Body = "Will fail",
        };

        var result = await sender.SendAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notifications.SendFailed");
        failingProvider.AttemptCount.Should().Be(3); // 1 initial + 2 retries
    }

    [Fact]
    public async Task SendAsync_ProviderFailsThenSucceeds_RecordMarkedSent()
    {
        var failingProvider = new FailingChannelProvider(NotificationChannel.Email, succeedOnAttempt: 2);
        var opts = Options.Create(new NotificationsSettings { MaxRetryCount = 3, RetryDelay = TimeSpan.Zero });
        var sender = new NotificationDispatcher([failingProvider], _templateEngine, _repository, opts);
        var request = new NotificationRequest
        {
            RecipientId = "user-7",
            RecipientAddress = "user@example.com",
            Channel = NotificationChannel.Email,
            Body = "Retry scenario",
        };

        var result = await sender.SendAsync(request);

        result.IsSuccess.Should().BeTrue();
        failingProvider.AttemptCount.Should().Be(2);
    }

    [Fact]
    public async Task GetStatusAsync_AfterSend_ReturnsSentRecord()
    {
        var sender = CreateSender();
        var request = new NotificationRequest
        {
            RecipientId = "user-8",
            RecipientAddress = "user@example.com",
            Channel = NotificationChannel.Email,
            Body = "Status check",
        };

        var sendResult = await sender.SendAsync(request);
        var statusResult = await sender.GetStatusAsync(sendResult.Value!);

        statusResult.IsSuccess.Should().BeTrue();
        statusResult.Value!.Status.Should().Be(NotificationStatus.Sent);
        statusResult.Value.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatusAsync_UnknownId_ReturnsNotFound()
    {
        var sender = CreateSender();

        var result = await sender.GetStatusAsync("nonexistent-id");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByRecipientAsync_AfterMultipleSends_ReturnsAllRecordsForRecipient()
    {
        var sender = CreateSender();
        NotificationRequest makeRequest(string recipientId) => new()
        {
            RecipientId = recipientId,
            RecipientAddress = "addr@example.com",
            Channel = NotificationChannel.Email,
            Body = "Test",
        };

        await sender.SendAsync(makeRequest("recipient-A"));
        await sender.SendAsync(makeRequest("recipient-A"));
        await sender.SendAsync(makeRequest("recipient-B"));

        var result = await sender.GetByRecipientAsync("recipient-A");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(r => r.RecipientId.Should().Be("recipient-A"));
    }

    [Fact]
    public async Task GetByRecipientAsync_WithPagination_ReturnsPagedResults()
    {
        var sender = CreateSender();
        for (var i = 0; i < 3; i++)
        {
            await sender.SendAsync(new NotificationRequest
            {
                RecipientId = "paged-user",
                RecipientAddress = "addr@example.com",
                Channel = NotificationChannel.Email,
                Body = $"Message {i}",
            });
        }

        var result = await sender.GetByRecipientAsync("paged-user", skip: 1, take: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendAsync_ExpiredNotification_ReturnsExpiredError()
    {
        var sender = CreateSender();
        var request = new NotificationRequest
        {
            RecipientId = "user-exp",
            RecipientAddress = "user@example.com",
            Channel = NotificationChannel.Email,
            Body = "Flash sale",
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1),
        };

        var result = await sender.SendAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notifications.Expired");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task SendAsync_NotExpiredNotification_Sends()
    {
        var sender = CreateSender();
        var request = new NotificationRequest
        {
            RecipientId = "user-fresh",
            RecipientAddress = "user@example.com",
            Channel = NotificationChannel.Email,
            Body = "Still valid",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        var result = await sender.SendAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_AfterSend_RecordHasUpdatedAt()
    {
        var sender = CreateSender();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var request = new NotificationRequest
        {
            RecipientId = "user-upd",
            RecipientAddress = "user@example.com",
            Channel = NotificationChannel.Email,
            Body = "Check timestamp",
        };

        var sendResult = await sender.SendAsync(request);
        var statusResult = await sender.GetStatusAsync(sendResult.Value!);

        statusResult.Value!.UpdatedAt.Should().NotBeNull();
        statusResult.Value.UpdatedAt!.Value.Should().BeAfter(before);
    }
}

file sealed class FailingChannelProvider : INotificationChannelProvider
{
    private readonly int _succeedOnAttempt;

    public FailingChannelProvider(NotificationChannel channel, int succeedOnAttempt)
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
                StangaNetLib.Core.Common.Error.Internal($"Simulated failure on attempt {AttemptCount}.")));
    }
}
