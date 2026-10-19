using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// `auth/me` / login exponen los permisos granulares (unión de los roles) y
/// los módulos habilitados del plan: el frontend los consume para ocultar
/// navegación/botones sin perder la validación autoritativa del backend.
/// </summary>
public sealed class AuthServicePermissionsTests
{
    private readonly Mock<IAuthRepository> _authRepo = new();
    private readonly Mock<IFirebaseAuthService> _firebase = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IMfaService> _mfa = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IModuleEntitlementService> _modules = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();
    private readonly AuthService _sut;
    private readonly string _tenantId = Guid.NewGuid().ToString();

    public AuthServicePermissionsTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(TenantId.FromString(_tenantId));
        _sut = new AuthService(_authRepo.Object, _firebase.Object, _jwt.Object, _mfa.Object,
            _tenant.Object, _email.Object, _modules.Object, _logger.Object);
    }

    private User MakeUserWithRoles() => new()
    {
        Id = Guid.NewGuid(),
        FirebaseUid = "fb-uid",
        Email = "test@zorvian.app",
        DisplayName = "Test",
        TenantId = _tenantId,
        UserRoles =
        {
            new UserRole
            {
                Role = new Role
                {
                    Name = RoleType.Supervisor,
                    DisplayName = "Supervisor",
                    RolePermissions =
                    {
                        new RolePermission { PermissionCode = "sale.write" },
                        new RolePermission { PermissionCode = "client.read" },
                    },
                },
            },
            new UserRole
            {
                Role = new Role
                {
                    Name = RoleType.Employee,
                    DisplayName = "Empleado",
                    RolePermissions =
                    {
                        // Duplicado entre roles → DISTINCT.
                        new RolePermission { PermissionCode = "sale.write" },
                        new RolePermission { PermissionCode = "inventory.read" },
                    },
                },
            },
        },
    };

    [Fact]
    public async Task GetMeAsync_ExposesDistinctPermissions_AndEnabledModules()
    {
        var user = MakeUserWithRoles();
        _authRepo.Setup(r => r.GetUserWithRolesAsync(user.Id)).ReturnsAsync(user);
        _authRepo.Setup(r => r.GetCompanyByTenantIdAsync(_tenantId))
            .ReturnsAsync(new Company { Id = Guid.NewGuid(), TenantId = _tenantId, SubscriptionPlan = "professional", Name = "X" });
        _modules.Setup(m => m.GetEnabledModulesAsync("professional"))
            .ReturnsAsync(new HashSet<ModuleType> { ModuleType.Sales, ModuleType.Fleet });

        var me = await _sut.GetMeAsync(user.Id);

        Assert.NotNull(me);
        Assert.Equal(new[] { "client.read", "inventory.read", "sale.write" }, me!.Permissions!.OrderBy(p => p));
        Assert.Equal(new[] { "Fleet", "Sales" }, me.EnabledModules!.OrderBy(m => m));
        Assert.Equal("professional", me.Plan);
    }

    [Fact]
    public async Task GetMeAsync_UserWithoutRoles_HasEmptyPermissions()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "norole@zorvian.app",
            DisplayName = "Sin Rol",
            TenantId = _tenantId,
        };
        _authRepo.Setup(r => r.GetUserWithRolesAsync(user.Id)).ReturnsAsync(user);
        _authRepo.Setup(r => r.GetCompanyByTenantIdAsync(_tenantId)).ReturnsAsync((Company?)null);
        _modules.Setup(m => m.GetEnabledModulesAsync("starter"))
            .ReturnsAsync(new HashSet<ModuleType>());

        var me = await _sut.GetMeAsync(user.Id);

        Assert.NotNull(me);
        Assert.Empty(me!.Permissions!);
        Assert.Equal("starter", me.Plan);
    }
}
