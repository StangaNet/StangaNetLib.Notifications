namespace StangaNetLib.Notifications.Tests;

public sealed class InMemoryNotificationChannelProviderTests
{
    private static NotificationRecord MakeRecord(string id = "rec-1") => new()
    {
        Id = id,
        RecipientId = "user-1",
        RecipientAddress = "addr",
        Channel = NotificationChannel.Email,
        Status = NotificationStatus.Pending,
        Body = "Test body",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    // ── Channel property ──────────────────────────────────────────────────

    [Fact]
    public void Channel_MatchesConstructorArgument()
    {
        var provider = new InMemoryNotificationChannelProvider(NotificationChannel.Push);
        provider.Channel.Should().Be(NotificationChannel.Push);
    }

    [Fact]
    public void Channel_DefaultsToInApp()
    {
        var provider = new InMemoryNotificationChannelProvider();
        provider.Channel.Should().Be(NotificationChannel.InApp);
    }

    // ── SendAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ReturnsSuccess()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new InMemoryNotificationChannelProvider(NotificationChannel.Email);
        var result   = await provider.SendAsync(MakeRecord(), ct);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_RecordAppearsInSentNotifications()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new InMemoryNotificationChannelProvider(NotificationChannel.Email);
        var record   = MakeRecord("r-100");

        await provider.SendAsync(record, ct);

        provider.SentNotifications.Should().ContainSingle(n => n.Id == "r-100");
    }

    [Fact]
    public async Task SendAsync_MultipleRecords_AllPresentInOrder()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new InMemoryNotificationChannelProvider(NotificationChannel.Email);

        await provider.SendAsync(MakeRecord("r-1"), ct);
        await provider.SendAsync(MakeRecord("r-2"), ct);
        await provider.SendAsync(MakeRecord("r-3"), ct);

        provider.SentNotifications.Select(n => n.Id)
            .Should().ContainInOrder("r-1", "r-2", "r-3");
    }

    // ── SentNotifications snapshot isolation ─────────────────────────────

    [Fact]
    public async Task SentNotifications_ReturnsSnapshot_SubsequentSendDoesNotMutatePriorSnapshot()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new InMemoryNotificationChannelProvider(NotificationChannel.Email);

        await provider.SendAsync(MakeRecord("snap-1"), ct);
        var snapshot = provider.SentNotifications;

        await provider.SendAsync(MakeRecord("snap-2"), ct);

        snapshot.Should().HaveCount(1, "snapshot taken before second send must not change");
        provider.SentNotifications.Should().HaveCount(2);
    }

    // ── Concurrent safety ─────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ConcurrentSends_AllRecordsPresent()
    {
        var ct       = TestContext.Current.CancellationToken;
        var provider = new InMemoryNotificationChannelProvider(NotificationChannel.Email);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(i =>
            provider.SendAsync(MakeRecord($"c-{i}"), ct)));

        provider.SentNotifications.Should().HaveCount(20);
    }
}
