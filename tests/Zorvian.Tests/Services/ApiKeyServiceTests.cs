using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

/// <summary>
/// Regresión del bug de API Keys: la validación corre en ApiKeyMiddleware
/// ANTES de que TenantMiddleware resuelva el tenant (contexto aún GUID-cero).
/// Con el query filter de ApiKey aplicado, la key nunca se encontraba → 401
/// siempre. La validación debe hallar la key aunque el contexto esté vacío.
/// </summary>
public sealed class ApiKeyServiceTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly ApiKeyService _sut;
    private readonly string _tenantId;

    public ApiKeyServiceTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Contexto "sin resolver": GUID-cero y sin bypass — como ocurre en
        // ApiKeyMiddleware (corre antes de TenantMiddleware).
        _tenant.Setup(t => t.TenantId).Returns(new TenantId(Guid.Empty));
        _tenant.Setup(t => t.BypassTenantFilter).Returns(false);

        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new ApiKeyService(_db);
    }

    [Fact]
    public async Task ValidateKey_FindsKey_WhenTenantContextNotResolvedYet()
    {
        var (rawKey, _) = await _sut.CreateApiKeyAsync("test-key", _tenantId);

        var info = await _sut.ValidateKeyAndGetInfoAsync(rawKey);

        Assert.NotNull(info);
        Assert.Equal(_tenantId, info!.Value.TenantId);
    }

    [Fact]
    public async Task ValidateKey_ReturnsUserId_WhenKeyHasOwner()
    {
        var userId = Guid.NewGuid();
        var rawKey = "zorvian-test-key-with-owner-00000001";
        _db.Set<ApiKey>().Add(new ApiKey
        {
            Name = "owned",
            KeyHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawKey))).ToLower(),
            Prefix = rawKey[..8],
            TenantId = _tenantId,
            UserId = userId,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var info = await _sut.ValidateKeyAndGetInfoAsync(rawKey);

        Assert.NotNull(info);
        Assert.Equal(userId, info!.Value.UserId);
    }

    [Fact]
    public async Task ValidateKey_ReturnsNull_ForUnknownKey()
    {
        var info = await _sut.ValidateKeyAndGetInfoAsync(new string('a', 64));
        Assert.Null(info);
    }

    [Fact]
    public async Task ValidateKey_ReturnsNull_ForExpiredKey()
    {
        var (rawKey, id) = await _sut.CreateApiKeyAsync("expired", _tenantId);
        var key = await _db.Set<ApiKey>().IgnoreQueryFilters().FirstAsync(k => k.Id == id);
        key.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await _db.SaveChangesAsync();

        var info = await _sut.ValidateKeyAndGetInfoAsync(rawKey);
        Assert.Null(info);
    }

    [Fact]
    public async Task ValidateKey_ReturnsNull_ForInactiveKey()
    {
        var (rawKey, id) = await _sut.CreateApiKeyAsync("inactive", _tenantId);
        var key = await _db.Set<ApiKey>().IgnoreQueryFilters().FirstAsync(k => k.Id == id);
        key.IsActive = false;
        await _db.SaveChangesAsync();

        var info = await _sut.ValidateKeyAndGetInfoAsync(rawKey);
        Assert.Null(info);
    }
}
