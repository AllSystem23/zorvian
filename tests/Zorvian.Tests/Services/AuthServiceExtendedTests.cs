using Moq;
using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;

namespace Zorvian.Tests.Services;

public sealed class AuthServiceExtendedTests
{
    private readonly Mock<IAuthRepository> _authRepo = new();
    private readonly Mock<IFirebaseAuthService> _firebase = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IMfaService> _mfa = new();
    private readonly AuthService _sut;

    public AuthServiceExtendedTests()
    {
        _mfa.Setup(m => m.GenerateMfaToken(It.IsAny<Guid>())).Returns("mfa-test-token");
        _mfa.Setup(m => m.ValidateMfaToken(It.IsAny<string>())).Returns((Guid?)null);
        _sut = new AuthService(_authRepo.Object, _firebase.Object, _jwt.Object, _mfa.Object);
    }

    private User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        FirebaseUid = "fb-uid",
        Email = "test@nexora.app",
        DisplayName = "Test",
        UserRoles = { new UserRole { Role = new Role { Name = RoleType.Employee, DisplayName = "Empleado" } } },
    };

    private void SetupJwt(User user)
    {
        _jwt.Setup(j => j.GenerateTokens(user, It.IsAny<Role>(), It.IsAny<string>()))
            .Returns(("access-token", "refresh-token", 3600));
    }

    [Fact]
    public async Task LoginAsync_PassesDeviceFingerprintToRefreshToken()
    {
        var fbUser = new FirebaseUser("fb-uid", "test@nexora.app", "Test", null);
        _firebase.Setup(f => f.VerifyIdTokenAsync("token")).ReturnsAsync(fbUser);
        var user = MakeUser();
        _authRepo.Setup(r => r.GetUserByFirebaseUidAsync("fb-uid")).ReturnsAsync(user);
        SetupJwt(user);

        var result = await _sut.LoginAsync(new LoginRequest("token", "device-fp-abc"));

        Assert.NotNull(result);
        _authRepo.Verify(r => r.AddRefreshTokenAsync(It.Is<RefreshToken>(rt =>
            rt.DeviceFingerprint == "device-fp-abc")), Times.Once);
    }

    [Fact]
    public async Task LoginWithPasswordAsync_PassesDeviceFingerprint()
    {
        var fbUser = new FirebaseUser("fb-uid-pw", "pw@nexora.app", "PW", null);
        _firebase.Setup(f => f.SignInWithPasswordAsync("pw@nexora.app", "pass")).ReturnsAsync(fbUser);
        var user = MakeUser();
        _authRepo.Setup(r => r.GetUserByFirebaseUidAsync("fb-uid-pw")).ReturnsAsync(user);
        SetupJwt(user);

        var result = await _sut.LoginWithPasswordAsync(new LoginPasswordRequest("pw@nexora.app", "pass", "fp-xyz"));

        Assert.NotNull(result);
        _authRepo.Verify(r => r.AddRefreshTokenAsync(It.Is<RefreshToken>(rt =>
            rt.DeviceFingerprint == "fp-xyz")), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser();
        user.Id = userId;
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "old-refresh-token",
            UserId = userId,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        _authRepo.Setup(r => r.GetRefreshTokenAsync("old-refresh-token")).ReturnsAsync(storedToken);
        SetupJwt(user);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest("old-refresh-token"));

        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
        Assert.True(storedToken.IsRevoked);
        Assert.NotNull(storedToken.RevokedAt);
        _authRepo.Verify(r => r.AddRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ReturnsNull()
    {
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "expired-token",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        };
        _authRepo.Setup(r => r.GetRefreshTokenAsync("expired-token")).ReturnsAsync(storedToken);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest("expired-token"));

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithNonExistentToken_ReturnsNull()
    {
        _authRepo.Setup(r => r.GetRefreshTokenAsync("ghost")).ReturnsAsync((RefreshToken?)null);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest("ghost"));

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReuseDetection_RevokesAllUserSessions()
    {
        var userId = Guid.NewGuid();
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "already-revoked",
            UserId = userId,
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            User = MakeUser(),
        };
        revokedToken.User.Id = userId;
        _authRepo.Setup(r => r.GetRefreshTokenAsync("already-revoked")).ReturnsAsync(revokedToken);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest("already-revoked"));

        Assert.Null(result);
        _authRepo.Verify(r => r.RevokeAllUserTokensAsync(userId, It.IsAny<string?>()), Times.Once);
        _authRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithDeviceFingerprint_PassesToNewToken()
    {
        var user = MakeUser();
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "old",
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        _authRepo.Setup(r => r.GetRefreshTokenAsync("old")).ReturnsAsync(storedToken);
        SetupJwt(user);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest("old", "new-device-fp"));

        Assert.NotNull(result);
        _authRepo.Verify(r => r.AddRefreshTokenAsync(It.Is<RefreshToken>(rt =>
            rt.DeviceFingerprint == "new-device-fp")), Times.Once);
    }

    [Fact]
    public async Task RevokeAllSessionsAsync_RevokesAllActiveTokens()
    {
        var userId = Guid.NewGuid();

        var result = await _sut.RevokeAllSessionsAsync(userId);

        Assert.True(result);
        _authRepo.Verify(r => r.RevokeAllUserTokensAsync(userId, It.IsAny<string?>()), Times.Once);
        _authRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMeAsync_WithValidUser_ReturnsUserInfo()
    {
        var user = MakeUser();
        _authRepo.Setup(r => r.GetUserWithRolesAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.GetMeAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal("Employee", result.Role);
    }

    [Fact]
    public async Task GetMeAsync_WithInvalidUser_ReturnsNull()
    {
        _authRepo.Setup(r => r.GetUserWithRolesAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var result = await _sut.GetMeAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
