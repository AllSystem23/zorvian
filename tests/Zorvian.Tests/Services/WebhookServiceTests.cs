using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

public sealed class WebhookServiceTests
{
    private readonly ZorvianDbContext _db;
    private readonly WebhookService _sut;
    private readonly string _tenantId;

    public WebhookServiceTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.Parse(_tenantId)));
        _db = new ZorvianDbContext(options, tenantMock.Object);

        var bgJobs = new Mock<Hangfire.IBackgroundJobClient>();
        _sut = new WebhookService(_db, bgJobs.Object);
    }

    [Fact]
    public async Task PublishAsync_WithActiveSubscription_ShouldCreateDeliveryLog()
    {
        var sub = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/webhook",
            Secret = "test-secret",
            IsActive = true,
            MaxRetries = 3,
            RetryIntervalSeconds = 60,
        };
        _db.Set<WebhookSubscription>().Add(sub);
        await _db.SaveChangesAsync();

        await _sut.PublishAsync(_tenantId, "sale.created", new { SaleId = "123" });

        var logs = await _db.Set<WebhookDeliveryLog>().ToListAsync();
        Assert.Single(logs);
        Assert.Equal(sub.Id, logs[0].SubscriptionId);
        Assert.Equal(sub.TargetUrl, logs[0].TargetUrl);
    }

    [Fact]
    public async Task PublishAsync_WithNoActiveSubscription_ShouldNotCreateLog()
    {
        var sub = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/webhook",
            Secret = "test-secret",
            IsActive = false,
            MaxRetries = 3,
            RetryIntervalSeconds = 60,
        };
        _db.Set<WebhookSubscription>().Add(sub);
        await _db.SaveChangesAsync();

        await _sut.PublishAsync(_tenantId, "sale.created", new { SaleId = "123" });

        var logs = await _db.Set<WebhookDeliveryLog>().ToListAsync();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task PublishAsync_WithWrongEventType_ShouldNotCreateLog()
    {
        var sub = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EventType = "purchase.created",
            TargetUrl = "https://example.com/webhook",
            Secret = "test-secret",
            IsActive = true,
            MaxRetries = 3,
            RetryIntervalSeconds = 60,
        };
        _db.Set<WebhookSubscription>().Add(sub);
        await _db.SaveChangesAsync();

        await _sut.PublishAsync(_tenantId, "sale.created", new { SaleId = "123" });

        var logs = await _db.Set<WebhookDeliveryLog>().ToListAsync();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleSubscriptions_ShouldCreateLogForEach()
    {
        for (int i = 0; i < 3; i++)
        {
            _db.Set<WebhookSubscription>().Add(new WebhookSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                EventType = "sale.created",
                TargetUrl = $"https://example{i}.com/webhook",
                Secret = "test-secret",
                IsActive = true,
                MaxRetries = 3,
                RetryIntervalSeconds = 60,
            });
        }
        await _db.SaveChangesAsync();

        await _sut.PublishAsync(_tenantId, "sale.created", new { SaleId = "123" });

        var logs = await _db.Set<WebhookDeliveryLog>().ToListAsync();
        Assert.Equal(3, logs.Count);
    }

    [Fact]
    public async Task PublishAsync_ShouldSerializePayloadAsJson()
    {
        var sub = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/webhook",
            Secret = "test-secret",
            IsActive = true,
            MaxRetries = 3,
            RetryIntervalSeconds = 60,
        };
        _db.Set<WebhookSubscription>().Add(sub);
        await _db.SaveChangesAsync();

        var payload = new { SaleId = "SALE-001", Total = 150.50m };
        await _sut.PublishAsync(_tenantId, "sale.created", payload);

        var log = await _db.Set<WebhookDeliveryLog>().FirstAsync();
        var deserialized = JsonSerializer.Deserialize<JsonElement>(log.PayloadJson!);
        Assert.Equal("SALE-001", deserialized.GetProperty("SaleId").GetString());
        Assert.Equal(150.50m, deserialized.GetProperty("Total").GetDecimal());
    }
}
