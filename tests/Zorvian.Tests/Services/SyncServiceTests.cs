using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

public sealed class SyncServiceTests
{
    private readonly ZorvianDbContext _db;
    private readonly SyncService _sut;
    private readonly string _tenantId;

    public SyncServiceTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.Parse(_tenantId)));
        _db = new ZorvianDbContext(options, tenantMock.Object);
        _sut = new SyncService(_db);
    }

    [Fact]
    public async Task JournalAsync_ShouldAppendEntry()
    {
        await _sut.JournalAsync("Product", "prod-1", "created", "{\"name\":\"Test\"}", _tenantId);

        var entries = await _db.Set<SyncJournal>().ToListAsync();
        Assert.Single(entries);
        Assert.Equal("Product", entries[0].EntityName);
        Assert.Equal("prod-1", entries[0].EntityId);
        Assert.Equal("created", entries[0].Operation);
    }

    [Fact]
    public async Task PullAsync_WithNoChanges_ShouldReturnEmpty()
    {
        var result = await _sut.PullAsync(new SyncPullRequest("Product", null, 500), _tenantId);

        Assert.Empty(result.Changes);
    }

    [Fact]
    public async Task PullAsync_WithChangesSinceMinDate_ShouldReturnAll()
    {
        await _sut.JournalAsync("Product", "p1", "created", "{\"name\":\"A\"}", _tenantId);
        await _sut.JournalAsync("Product", "p2", "updated", "{\"name\":\"B\"}", _tenantId);

        var result = await _sut.PullAsync(new SyncPullRequest("Product", DateTime.MinValue, 500), _tenantId);

        Assert.Equal(2, result.Changes.Count);
    }

    [Fact]
    public async Task PullAsync_ShouldFilterByEntityName()
    {
        await _sut.JournalAsync("Product", "p1", "created", null, _tenantId);
        await _sut.JournalAsync("Sale", "s1", "created", null, _tenantId);

        var result = await _sut.PullAsync(new SyncPullRequest("Product", DateTime.MinValue, 500), _tenantId);

        Assert.Single(result.Changes);
    }

    [Fact]
    public async Task PullAsync_ShouldRespectSinceParameter()
    {
        await _sut.JournalAsync("Product", "p1", "created", null, _tenantId);

        var later = DateTime.UtcNow.AddMinutes(1);
        var result = await _sut.PullAsync(new SyncPullRequest("Product", later, 500), _tenantId);

        Assert.Empty(result.Changes);
    }

    [Fact]
    public async Task PullAsync_ShouldRespectTakeParameter()
    {
        for (var i = 0; i < 10; i++)
            await _sut.JournalAsync("Product", $"p{i}", "created", null, _tenantId);

        var result = await _sut.PullAsync(new SyncPullRequest("Product", DateTime.MinValue, 3), _tenantId);

        Assert.Equal(3, result.Changes.Count);
    }

    [Fact]
    public async Task PushAsync_ShouldJournalAllMutations()
    {
        var mutations = new List<SyncPushRequest>
        {
            new("Product", "p1", "created", "{\"name\":\"A\"}", "c1"),
            new("Product", "p2", "updated", "{\"name\":\"B\"}", "c2"),
        };

        var result = await _sut.PushAsync(mutations, _tenantId);

        Assert.Empty(result.Conflicts);
        var entries = await _db.Set<SyncJournal>().ToListAsync();
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public async Task PullAsync_ShouldReturnServerTimestamp()
    {
        var result = await _sut.PullAsync(new SyncPullRequest("Product", null, 500), _tenantId);

        Assert.True(result.ServerTimestamp > DateTime.MinValue);
    }
}
