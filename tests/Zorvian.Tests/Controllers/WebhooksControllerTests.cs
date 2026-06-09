using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Controllers;

namespace Zorvian.Tests.Controllers;

public sealed class WebhooksControllerTests : IDisposable
{
    private readonly ZorvianDbContext _db;
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly WebhooksController _sut;
    private readonly string _tenantId;

    public WebhooksControllerTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        _tenant.Setup(t => t.TenantId).Returns(new TenantId(Guid.Parse(_tenantId)));

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new WebhooksController(_db, _tenant.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetSubscriptions_ShouldReturnAll()
    {
        _db.Set<WebhookSubscription>().AddRange(
            new WebhookSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                EventType = "sale.created",
                TargetUrl = "https://a.com/w",
                Secret = "s1",
                IsActive = true,
                MaxRetries = 3,
                RetryIntervalSeconds = 60,
            },
            new WebhookSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                EventType = "purchase.created",
                TargetUrl = "https://b.com/w",
                Secret = "s2",
                IsActive = true,
                MaxRetries = 3,
                RetryIntervalSeconds = 60,
            });
        await _db.SaveChangesAsync();

        var result = await _sut.GetSubscriptions();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var subs = ok.Value.Should().BeAssignableTo<List<WebhookSubscription>>().Subject;
        subs.Should().HaveCount(2);
    }

    [Fact]
    public async Task Subscribe_ShouldCreateAndReturnSecret()
    {
        var request = new CreateWebhookRequest(
            "sale.created",
            "https://example.com/webhook",
            "Test webhook",
            5,
            30);

        var result = await _sut.Subscribe(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var sub = ok.Value.Should().BeAssignableTo<WebhookSubscription>().Subject;
        sub.EventType.Should().Be("sale.created");
        sub.TargetUrl.Should().Be("https://example.com/webhook");
        sub.Secret.Should().NotBeNullOrEmpty();
        sub.MaxRetries.Should().Be(5);
        sub.RetryIntervalSeconds.Should().Be(30);
        sub.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSubscription_ShouldModifyFields()
    {
        var sub = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EventType = "sale.created",
            TargetUrl = "https://old.com/w",
            Secret = "old-secret",
            IsActive = true,
            MaxRetries = 3,
            RetryIntervalSeconds = 60,
        };
        _db.Set<WebhookSubscription>().Add(sub);
        await _db.SaveChangesAsync();

        var request = new UpdateWebhookRequest(
            "purchase.created",
            "https://new.com/w",
            "Updated",
            true,
            5,
            120);

        var result = await _sut.UpdateSubscription(sub.Id, request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var updated = ok.Value.Should().BeAssignableTo<WebhookSubscription>().Subject;
        updated.EventType.Should().Be("purchase.created");
        updated.TargetUrl.Should().Be("https://new.com/w");
        updated.MaxRetries.Should().Be(5);
        updated.RetryIntervalSeconds.Should().Be(120);
        updated.Description.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateSubscription_NonExistent_ShouldReturn404()
    {
        var request = new UpdateWebhookRequest("s", "http://x.com", null, true, 3, 60);
        var result = await _sut.UpdateSubscription(Guid.NewGuid(), request);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Unsubscribe_ShouldRemove()
    {
        var sub = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/w",
            Secret = "s",
            IsActive = true,
            MaxRetries = 3,
            RetryIntervalSeconds = 60,
        };
        _db.Set<WebhookSubscription>().Add(sub);
        await _db.SaveChangesAsync();

        var result = await _sut.Unsubscribe(sub.Id);

        result.Should().BeOfType<NoContentResult>();
        var exists = await _db.Set<WebhookSubscription>().AnyAsync(s => s.Id == sub.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Unsubscribe_NonExistent_ShouldReturn404()
    {
        var result = await _sut.Unsubscribe(Guid.NewGuid());
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RegenerateSecret_ShouldGenerateNewSecret()
    {
        var sub = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/w",
            Secret = "original-secret",
            IsActive = true,
            MaxRetries = 3,
            RetryIntervalSeconds = 60,
        };
        _db.Set<WebhookSubscription>().Add(sub);
        await _db.SaveChangesAsync();

        var result = await _sut.RegenerateSecret(sub.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var updated = ok.Value.Should().BeAssignableTo<WebhookSubscription>().Subject;
        updated.Secret.Should().NotBe("original-secret");
        updated.Secret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegenerateSecret_NonExistent_ShouldReturn404()
    {
        var result = await _sut.RegenerateSecret(Guid.NewGuid());
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetDeliveryLogs_ShouldReturnAllLogs()
    {
        var subId = Guid.NewGuid();
        _db.Set<WebhookSubscription>().Add(new WebhookSubscription
        {
            Id = subId,
            TenantId = _tenantId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/w",
            Secret = "s",
            IsActive = true,
            MaxRetries = 3,
            RetryIntervalSeconds = 60,
        });
        _db.Set<WebhookDeliveryLog>().AddRange(
            new WebhookDeliveryLog
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subId,
                EventType = "sale.created",
                TargetUrl = "https://example.com/w",
                Attempt = 1,
                MaxRetries = 3,
                Success = true,
                HttpStatusCode = 200,
                PayloadJson = "{}",
                TenantId = _tenantId,
            },
            new WebhookDeliveryLog
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subId,
                EventType = "sale.created",
                TargetUrl = "https://example.com/w",
                Attempt = 1,
                MaxRetries = 3,
                Success = false,
                HttpStatusCode = 500,
                PayloadJson = "{}",
                TenantId = _tenantId,
            });
        await _db.SaveChangesAsync();

        var result = await _sut.GetDeliveryLogs(null, 1, 50);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var logs = ok.Value.Should().BeAssignableTo<List<WebhookDeliveryLog>>().Subject;
        logs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDeliveryLogs_WithSubscriptionFilter_ShouldFilter()
    {
        var subId1 = Guid.NewGuid();
        var subId2 = Guid.NewGuid();
        _db.Set<WebhookSubscription>().AddRange(
            new WebhookSubscription { Id = subId1, TenantId = _tenantId, EventType = "a", TargetUrl = "http://a.com", Secret = "s", IsActive = true, MaxRetries = 3, RetryIntervalSeconds = 60 },
            new WebhookSubscription { Id = subId2, TenantId = _tenantId, EventType = "b", TargetUrl = "http://b.com", Secret = "s", IsActive = true, MaxRetries = 3, RetryIntervalSeconds = 60 });
        _db.Set<WebhookDeliveryLog>().AddRange(
            new WebhookDeliveryLog { Id = Guid.NewGuid(), SubscriptionId = subId1, EventType = "a", TargetUrl = "http://a.com", Attempt = 1, MaxRetries = 3, Success = true, PayloadJson = "{}", TenantId = _tenantId },
            new WebhookDeliveryLog { Id = Guid.NewGuid(), SubscriptionId = subId2, EventType = "b", TargetUrl = "http://b.com", Attempt = 1, MaxRetries = 3, Success = true, PayloadJson = "{}", TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var result = await _sut.GetDeliveryLogs(subId1, 1, 50);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var logs = ok.Value.Should().BeAssignableTo<List<WebhookDeliveryLog>>().Subject;
        logs.Should().HaveCount(1);
        logs[0].SubscriptionId.Should().Be(subId1);
    }

    [Fact]
    public async Task GetSubscriptionLogs_ShouldReturnLogsForSubscription()
    {
        var subId = Guid.NewGuid();
        _db.Set<WebhookSubscription>().Add(new WebhookSubscription
        {
            Id = subId,
            TenantId = _tenantId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/w",
            Secret = "s",
            IsActive = true,
            MaxRetries = 3,
            RetryIntervalSeconds = 60,
        });
        _db.Set<WebhookDeliveryLog>().Add(new WebhookDeliveryLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/w",
            Attempt = 1,
            MaxRetries = 3,
            Success = true,
            PayloadJson = "{}",
            TenantId = _tenantId,
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetSubscriptionLogs(subId, 1, 50);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var logs = ok.Value.Should().BeAssignableTo<List<WebhookDeliveryLog>>().Subject;
        logs.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSubscriptionLogs_NonExistent_ShouldReturn404()
    {
        var result = await _sut.GetSubscriptionLogs(Guid.NewGuid(), 1, 50);
        result.Should().BeOfType<NotFoundResult>();
    }
}
