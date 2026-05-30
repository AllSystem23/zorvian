using Microsoft.EntityFrameworkCore;
using Nexora.Infrastructure.Services;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;
using Moq;
using Nexora.Core.Interfaces;

namespace Nexora.Tests.Services;

public sealed class ApiKeyServiceTests
{
    private readonly NexoraDbContext _db;
    private readonly ApiKeyService _sut;

    public ApiKeyServiceTests()
    {
        var options = new DbContextOptionsBuilder<NexoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns("tenant-1");
        _db = new NexoraDbContext(options, tenantMock.Object);
        _sut = new ApiKeyService(_db);
    }

    [Fact]
    public async Task CreateApiKeyAsync_Should_Generate_Key_And_Hash_It()
    {
        var name = "External System";
        var tenantId = "tenant-1";

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
    public async Task ValidateKeyAsync_Should_Return_TenantId_For_Valid_Key()
    {
        var tenantId = "tenant-1";
        var (rawKey, _) = await _sut.CreateApiKeyAsync("Test", tenantId);

        var result = await _sut.ValidateKeyAsync(rawKey);

        Assert.Equal(tenantId, result);
    }

    [Fact]
    public async Task ValidateKeyAsync_Should_Return_Null_For_Invalid_Key()
    {
        var result = await _sut.ValidateKeyAsync("invalid-key-that-is-long-enough-to-pass-length-check");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateKeyAsync_Should_Return_Null_For_Expired_Key()
    {
        var tenantId = "tenant-1";
        var (rawKey, id) = await _sut.CreateApiKeyAsync("Test", tenantId, DateTime.UtcNow.AddDays(-1));

        var result = await _sut.ValidateKeyAsync(rawKey);

        Assert.Null(result);
    }
}
