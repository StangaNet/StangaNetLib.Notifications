using FluentAssertions;
using StangaNetLib.Notifications.Implementations;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Tests;

public sealed class InboxServiceTests
{
    [Fact]
    public async Task MarkAsRead_And_UnreadCount_Work()
    {
        var ct = TestContext.Current.CancellationToken;
        var repo = new InMemoryNotificationRepository();
        var inbox = new InboxService(repo);

        var record = new NotificationRecord
        {
            Id = "n1",
            RecipientId = "u1",
            RecipientAddress = "u1",
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Sent,
            Body = "Hello",
            CreatedAt = DateTimeOffset.UtcNow,
            IsRead = false,
        };

        (await repo.SaveAsync(record, ct)).IsSuccess.Should().BeTrue();
        (await inbox.GetUnreadCountAsync("u1", ct)).Value.Should().Be(1);
        (await inbox.MarkAsReadAsync("n1", ct)).IsSuccess.Should().BeTrue();
        (await inbox.GetUnreadCountAsync("u1", ct)).Value.Should().Be(0);

        var unreadOnly = await inbox.GetInboxAsync("u1", unreadOnly: true, cancellationToken: ct);
        unreadOnly.Value!.Should().BeEmpty();
    }
}
