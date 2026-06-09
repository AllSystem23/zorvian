using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Jobs;

namespace Zorvian.Tests.Services;

public sealed class WebhookDeliveryJobTests
{
    private readonly string _dbName;
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<HttpMessageHandler> _httpHandler = new();
    private readonly HttpClient _httpClient;
    private readonly WebhookDeliveryJob _sut;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _tenantId;

    public WebhookDeliveryJobTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        _dbName = Guid.NewGuid().ToString();

        _httpClient = new HttpClient(_httpHandler.Object);
        _httpClientFactory.Setup(f => f.CreateClient("Webhooks")).Returns(_httpClient);

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.Parse(_tenantId)));

        var bgJobs = new Mock<Hangfire.IBackgroundJobClient>();

        var services = new ServiceCollection();
        services.AddDbContext<ZorvianDbContext>(o =>
            o.UseInMemoryDatabase(_dbName));
        services.AddScoped(_ => tenantMock.Object);
        services.AddScoped(_ => _httpClientFactory.Object);
        services.AddScoped(_ => bgJobs.Object);
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        _sut = new WebhookDeliveryJob(_scopeFactory);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldMarkLogAsSuccess()
    {
        var (subId, logId) = await SeedSubscriptionAndLog();

        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        await _sut.ExecuteAsync(subId, logId, "sale.created", new { SaleId = "123" }, 3, 60);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        var log = await db.Set<WebhookDeliveryLog>().FindAsync(logId);
        Assert.NotNull(log);
        Assert.True(log.Success);
        Assert.Equal(200, log.HttpStatusCode);
        Assert.Equal(1, log.Attempt);
        Assert.Null(log.NextRetryAt);
    }

    [Fact]
    public async Task ExecuteAsync_OnHttpError_ShouldSetNextRetryAt()
    {
        var (subId, logId) = await SeedSubscriptionAndLog();

        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await _sut.ExecuteAsync(subId, logId, "sale.created", new { SaleId = "123" }, 3, 60);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        var log = await db.Set<WebhookDeliveryLog>().FindAsync(logId);
        Assert.NotNull(log);
        Assert.False(log.Success);
        Assert.Equal(500, log.HttpStatusCode);
        Assert.Equal(1, log.Attempt);
        Assert.NotNull(log.NextRetryAt);
        Assert.True(log.NextRetryAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ExecuteAsync_OnException_ShouldSetNextRetryAt()
    {
        var (subId, logId) = await SeedSubscriptionAndLog();

        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        await _sut.ExecuteAsync(subId, logId, "sale.created", new { SaleId = "123" }, 3, 60);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        var log = await db.Set<WebhookDeliveryLog>().FindAsync(logId);
        Assert.NotNull(log);
        Assert.False(log.Success);
        Assert.Equal(1, log.Attempt);
        Assert.NotNull(log.NextRetryAt);
    }

    [Fact]
    public async Task ExecuteAsync_AfterMaxRetries_ShouldNotSetNextRetryAt()
    {
        var (subId, logId) = await SeedSubscriptionAndLog(attempt: 2, maxRetries: 3);

        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await _sut.ExecuteAsync(subId, logId, "sale.created", new { SaleId = "123" }, 3, 60);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        var log = await db.Set<WebhookDeliveryLog>().FindAsync(logId);
        Assert.NotNull(log);
        Assert.False(log.Success);
        Assert.Equal(3, log.Attempt);
        Assert.Null(log.NextRetryAt);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeHmacSignatureHeader()
    {
        var secret = "test-secret-123";
        var subId = Guid.NewGuid();
        var logId = Guid.NewGuid();

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
            db.Set<WebhookSubscription>().Add(new WebhookSubscription
            {
                Id = subId,
                TenantId = _tenantId,
                EventType = "sale.created",
                TargetUrl = "https://example.com/webhook",
                Secret = secret,
                IsActive = true,
                MaxRetries = 3,
                RetryIntervalSeconds = 60,
            });
            db.Set<WebhookDeliveryLog>().Add(new WebhookDeliveryLog
            {
                Id = logId,
                SubscriptionId = subId,
                EventType = "sale.created",
                TargetUrl = "https://example.com/webhook",
                PayloadJson = """{"SaleId":"123"}""",
                Attempt = 0,
                MaxRetries = 3,
                TenantId = _tenantId,
            });
            await db.SaveChangesAsync();
        }

        HttpRequestMessage? capturedRequest = null;
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        await _sut.ExecuteAsync(subId, logId, "sale.created", new { SaleId = "123" }, 3, 60);

        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Contains("X-Zorvian-Signature"));

        var signature = capturedRequest.Headers.GetValues("X-Zorvian-Signature").First();
        Assert.False(string.IsNullOrEmpty(signature));
        Assert.Equal(64, signature.Length);
        Assert.Matches("^[0-9a-f]{64}$", signature);
    }

    [Fact]
    public async Task ExecuteAsync_WithInactiveSubscription_ShouldNotSend()
    {
        var subId = Guid.NewGuid();
        var logId = Guid.NewGuid();

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
            db.Set<WebhookSubscription>().Add(new WebhookSubscription
            {
                Id = subId,
                TenantId = _tenantId,
                EventType = "sale.created",
                TargetUrl = "https://example.com/webhook",
                Secret = "test",
                IsActive = false,
                MaxRetries = 3,
                RetryIntervalSeconds = 60,
            });
            db.Set<WebhookDeliveryLog>().Add(new WebhookDeliveryLog
            {
                Id = logId,
                SubscriptionId = subId,
                EventType = "sale.created",
                TargetUrl = "https://example.com/webhook",
                PayloadJson = "{}",
                Attempt = 0,
                MaxRetries = 3,
                TenantId = _tenantId,
            });
            await db.SaveChangesAsync();
        }

        await _sut.ExecuteAsync(subId, logId, "sale.created", new { }, 3, 60);

        using var checkScope = _scopeFactory.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        var log = await checkDb.Set<WebhookDeliveryLog>().FindAsync(logId);
        Assert.NotNull(log);
        Assert.Equal(0, log.Attempt);
    }

    private async Task<(Guid subId, Guid logId)> SeedSubscriptionAndLog(int attempt = 0, int maxRetries = 3)
    {
        var subId = Guid.NewGuid();
        var logId = Guid.NewGuid();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        db.Set<WebhookSubscription>().Add(new WebhookSubscription
        {
            Id = subId,
            TenantId = _tenantId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/webhook",
            Secret = "test-secret",
            IsActive = true,
            MaxRetries = maxRetries,
            RetryIntervalSeconds = 60,
        });
        db.Set<WebhookDeliveryLog>().Add(new WebhookDeliveryLog
        {
            Id = logId,
            SubscriptionId = subId,
            EventType = "sale.created",
            TargetUrl = "https://example.com/webhook",
            PayloadJson = """{"SaleId":"123"}""",
            Attempt = attempt,
            MaxRetries = maxRetries,
            TenantId = _tenantId,
        });
        await db.SaveChangesAsync();
        return (subId, logId);
    }
}
