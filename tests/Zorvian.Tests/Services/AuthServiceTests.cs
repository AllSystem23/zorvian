using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _authRepo = new();
    private readonly Mock<IFirebaseAuthService> _firebase = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IMfaService> _mfa = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _mfa.Setup(m => m.GenerateMfaToken(It.IsAny<Guid>())).Returns("mfa-test-token");
        _mfa.Setup(m => m.ValidateMfaToken(It.IsAny<string>())).Returns((Guid?)null);
        _tenant.Setup(t => t.TenantId).Returns(TenantId.FromString(Guid.NewGuid().ToString()));
        _tenant.Setup(t => t.IsSuperAdmin).Returns(false);
        _sut = new AuthService(_authRepo.Object, _firebase.Object, _jwt.Object, _mfa.Object, _tenant.Object, _email.Object, _logger.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidFirebaseToken_ReturnsAuthResponse()
    {
        var fbUser = new FirebaseUser("fb-uid-123", "test@nexora.app", "Test User", null);
        _firebase.Setup(f => f.VerifyIdTokenAsync("valid-token")).ReturnsAsync(fbUser);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirebaseUid = "fb-uid-123",
            Email = "test@nexora.app",
            DisplayName = "Test User",
            UserRoles = { new UserRole { Role = new Role { Name = RoleType.Employee, DisplayName = "Empleado" } } },
        };
        _authRepo.Setup(r => r.GetUserByFirebaseUidAsync("fb-uid-123")).ReturnsAsync(user);
        _jwt.Setup(j => j.GenerateTokens(user, It.IsAny<Role>(), It.IsAny<string>()))
            .Returns(("access-token", "refresh-token", 3600));

        var result = await _sut.LoginAsync(new LoginRequest("valid-token"));

        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal("test@nexora.app", result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidFirebaseToken_ReturnsNull()
    {
        _firebase.Setup(f => f.VerifyIdTokenAsync("invalid-token")).ReturnsAsync((FirebaseUser?)null);

        var result = await _sut.LoginAsync(new LoginRequest("invalid-token"));

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_CreatesNewUser_WhenFirstTime()
    {
        var fbUser = new FirebaseUser("new-fb-uid", "new@nexora.app", "New User", null);
        _firebase.Setup(f => f.VerifyIdTokenAsync("new-token")).ReturnsAsync(fbUser);

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            FirebaseUid = "new-fb-uid",
            Email = "new@nexora.app",
            DisplayName = "New User",
            UserRoles = { new UserRole { Role = new Role { Name = RoleType.Employee, DisplayName = "Empleado" } } },
        };

        var callCount = 0;
        _authRepo.Setup(r => r.GetUserByFirebaseUidAsync("new-fb-uid"))
            .ReturnsAsync(() => callCount++ == 0 ? null : newUser);

        _jwt.Setup(j => j.GenerateTokens(It.IsAny<User>(), It.IsAny<Role>(), It.IsAny<string>()))
            .Returns(("access-token", "refresh-token", 3600));

        var result = await _sut.LoginAsync(new LoginRequest("new-token"));

        Assert.NotNull(result);
        _authRepo.Verify(r => r.AddUserAsync(It.Is<User>(u => u.FirebaseUid == "new-fb-uid")), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_RevokesRefreshToken()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "valid-refresh-token",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        _authRepo.Setup(r => r.GetRefreshTokenAsync("valid-refresh-token")).ReturnsAsync(token);

        var result = await _sut.LogoutAsync("valid-refresh-token");

        Assert.True(result);
        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAt);
        _authRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidToken_ReturnsFalse()
    {
        _authRepo.Setup(r => r.GetRefreshTokenAsync("invalid")).ReturnsAsync((RefreshToken?)null);

        var result = await _sut.LogoutAsync("invalid");

        Assert.False(result);
    }

    [Fact]
    public async Task SwitchTenantAsync_WhenSuperAdminAndCompanyExists_ReturnsAuthResponse()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid().ToString();
        var superAdminRole = new Role { Id = Guid.NewGuid(), Name = RoleType.SuperAdmin, DisplayName = "Super Admin" };
        var user = new User
        {
            Id = userId,
            Email = "super@zorvian.app",
            DisplayName = "Super Admin",
            UserRoles = { new UserRole { RoleId = superAdminRole.Id, Role = superAdminRole } },
        };
        var company = new Company { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Company A" };

        _authRepo.Setup(r => r.GetUserWithRolesAsync(userId)).ReturnsAsync(user);
        _authRepo.Setup(r => r.GetCompaniesByTenantIdsAsync(It.Is<List<string>>(ids => ids.Contains(tenantId))))
            .ReturnsAsync([company]);
        _authRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _jwt.Setup(j => j.GenerateTokens(user, superAdminRole, tenantId))
            .Returns(("access-token", "refresh-token", 3600));

        var result = await _sut.SwitchTenantAsync(userId, tenantId);

        Assert.NotNull(result);
        Assert.Equal(tenantId, result.User.TenantId);
        _authRepo.Verify(r => r.UserHasTenantAccessAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SwitchTenantAsync_WhenCompanyDoesNotExist_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = userId,
            UserRoles = { new UserRole { Role = new Role { Name = RoleType.SuperAdmin, DisplayName = "Super Admin" } } },
        };

        _authRepo.Setup(r => r.GetUserWithRolesAsync(userId)).ReturnsAsync(user);
        _authRepo.Setup(r => r.GetCompaniesByTenantIdsAsync(It.Is<List<string>>(ids => ids.Contains(tenantId))))
            .ReturnsAsync([]);

        var result = await _sut.SwitchTenantAsync(userId, tenantId);

        Assert.Null(result);
    }
}
