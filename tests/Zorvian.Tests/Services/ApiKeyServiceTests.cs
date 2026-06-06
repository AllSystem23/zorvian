using Microsoft.EntityFrameworkCore;
using Zorvian.Infrastructure.Services;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Moq;
using Zorvian.Core.Interfaces;
using Xunit;

namespace Zorvian.Tests.Services;

public sealed class ApiKeyServiceTests
{
    private readonly ZorvianDbContext _db;
    private readonly ApiKeyService _sut;
    private readonly TenantId _testTenantId;

    public ApiKeyServiceTests()
    {
        _testTenantId = new TenantId(Guid.NewGuid());
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(_testTenantId);
        _db = new ZorvianDbContext(options, tenantMock.Object);
        _sut = new ApiKeyService(_db);
    }

    [Fact]
    public async Task CreateApiKeyAsync_Should_Generate_Key_And_Hash_It()
    {
        var name = "External System";
        var tenantId = _testTenantId.ToString();

        var (rawKey, id) = await _sut.CreateApiKeyAsync(name, tenantId);

        Assert.NotNull(rawKey);
        Assert.True(rawKey.Length >= 32);
        
        var apiKey = await _db.Set<ApiKey>().FindAsync(id);
        Assert.NotNull(apiKey);
        Assert.Equal(name, apiKey.Name);
        Assert.Equal(rawKey[..8], apiKey.Prefix);
        Assert.NotEqual(rawKey, apiKey.KeyHash); // Should be hashed
    }

    [Fact]
    public async Task ValidateKeyAndGetInfoAsync_Should_Return_TenantId_For_Valid_Key()
    {
        var tenantId = _testTenantId.ToString();
        var (rawKey, _) = await _sut.CreateApiKeyAsync("Test", tenantId);

        var result = await _sut.ValidateKeyAndGetInfoAsync(rawKey);

        Assert.NotNull(result);
        Assert.Equal(tenantId, result.Value.TenantId);
    }

    [Fact]
    public async Task ValidateKeyAndGetInfoAsync_Should_Return_Null_For_Invalid_Key()
    {
        var result = await _sut.ValidateKeyAndGetInfoAsync("invalid-key-that-is-long-enough-to-pass-length-check");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateKeyAndGetInfoAsync_Should_Return_Null_For_Expired_Key()
    {
        var tenantId = _testTenantId.ToString();
        var (rawKey, id) = await _sut.CreateApiKeyAsync("Test", tenantId, DateTime.UtcNow.AddDays(-1));

        var result = await _sut.ValidateKeyAndGetInfoAsync(rawKey);

        Assert.Null(result);
    }
}
