using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

public sealed class AuthRepositoryExtendedTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly AuthRepository _sut;

    public AuthRepositoryExtendedTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _tenant.Setup(t => t.TenantId).Returns("tenant-1");
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new AuthRepository(_db);
    }

    [Fact]
    public async Task GetActiveRefreshTokensAsync_ReturnsOnlyNonRevokedAndNotExpired()
    {
        var userId = Guid.NewGuid();
        _db.RefreshTokens.AddRange(
            new RefreshToken { UserId = userId, Token = "valid-1", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { UserId = userId, Token = "revoked", IsRevoked = true, ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { UserId = userId, Token = "expired", ExpiresAt = DateTime.UtcNow.AddDays(-1) },
            new RefreshToken { UserId = Guid.NewGuid(), Token = "other-user", ExpiresAt = DateTime.UtcNow.AddDays(1) }
        );
        await _db.SaveChangesAsync();

        var active = await _sut.GetActiveRefreshTokensAsync(userId);

        Assert.Single(active);
        Assert.Equal("valid-1", active[0].Token);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_RevokesAllActiveTokens()
    {
        var userId = Guid.NewGuid();
        _db.RefreshTokens.AddRange(
            new RefreshToken { UserId = userId, Token = "t1", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { UserId = userId, Token = "t2", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { UserId = userId, Token = "already-revoked", IsRevoked = true, ExpiresAt = DateTime.UtcNow.AddDays(1) }
        );
        await _db.SaveChangesAsync();

        await _sut.RevokeAllUserTokensAsync(userId);

        var all = await _db.RefreshTokens.IgnoreQueryFilters().ToListAsync();
        Assert.All(all, t => Assert.True(t.IsRevoked));
        Assert.All(all.Where(t => t.Token != "already-revoked"), t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithExcludeToken_SkipsSpecifiedToken()
    {
        var userId = Guid.NewGuid();
        _db.RefreshTokens.AddRange(
            new RefreshToken { UserId = userId, Token = "keep-me", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { UserId = userId, Token = "revoke-me", ExpiresAt = DateTime.UtcNow.AddDays(1) }
        );
        await _db.SaveChangesAsync();

        await _sut.RevokeAllUserTokensAsync(userId, "keep-me");

        var kept = await _db.RefreshTokens.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Token == "keep-me");
        Assert.False(kept!.IsRevoked);

        var revoked = await _db.RefreshTokens.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Token == "revoke-me");
        Assert.True(revoked!.IsRevoked);
    }

    [Fact]
    public async Task AddUserAsync_AddsUserToDatabase()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirebaseUid = "new-fb",
            Email = "new@test.com",
            DisplayName = "New",
            TenantId = "tenant-1",
        };

        await _sut.AddUserAsync(user);
        await _sut.SaveChangesAsync();

        var saved = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(saved);
        Assert.Equal("new@test.com", saved.Email);
    }
}
