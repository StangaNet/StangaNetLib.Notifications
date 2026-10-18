using StangaNetLib.Core.Common;

namespace StangaNetLib.Notifications.Tests;

public sealed class InMemoryNotificationRepositoryTests
{
    private readonly InMemoryNotificationRepository _repo = new();

    private static NotificationRecord MakeRecord(string id, string recipientId = "user-1") => new()
    {
        Id = id,
        RecipientId = recipientId,
        RecipientAddress = "addr",
        Channel = NotificationChannel.Email,
        Status = NotificationStatus.Pending,
        Body = "Test body",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task SaveAndGetById_ReturnsStoredRecord()
    {
        var record = MakeRecord("rec-1");
        await _repo.SaveAsync(record);

        var result = await _repo.GetByIdAsync("rec-1");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("rec-1");
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var result = await _repo.GetByIdAsync("unknown");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ChangesStoredRecord()
    {
        var record = MakeRecord("rec-2");
        await _repo.SaveAsync(record);

        record.Status = NotificationStatus.Sent;
        record.SentAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(record);

        var updated = await _repo.GetByIdAsync("rec-2");
        updated.Value!.Status.Should().Be(NotificationStatus.Sent);
        updated.Value.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByRecipient_ReturnsOnlyMatchingRecords()
    {
        await _repo.SaveAsync(MakeRecord("r-a", "alice"));
        await _repo.SaveAsync(MakeRecord("r-b", "alice"));
        await _repo.SaveAsync(MakeRecord("r-c", "bob"));

        var result = await _repo.GetByRecipientAsync("alice");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(r => r.RecipientId.Should().Be("alice"));
    }

    [Fact]
    public async Task GetByRecipient_NoRecords_ReturnsEmptyList()
    {
        var result = await _repo.GetByRecipientAsync("nobody");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByRecipient_WithSkipAndTake_ReturnsPaginatedRecords()
    {
        await _repo.SaveAsync(MakeRecord("p-1", "paginated"));
        await _repo.SaveAsync(MakeRecord("p-2", "paginated"));
        await _repo.SaveAsync(MakeRecord("p-3", "paginated"));

        var result = await _repo.GetByRecipientAsync("paginated", skip: 1, take: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
    }
}
