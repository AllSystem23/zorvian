using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Controllers;

namespace Zorvian.Tests.Authorization;

/// <summary>
/// `UsersController`: asignación de roles granular.
/// - `GET users/roles` serializa `Name` (enum `RoleType`) como STRING: sin
///   `JsonStringEnumConverter` viajaba numérico y el frontend no podía
///   compararlo (la página de usuarios reventaba al parsear).
/// - `PUT users/{id}/role` se apoya SOLO en `user.manage` (un rol personalizado
///   con ese permiso puede asignar roles) y bloquea la escalada de privilegios:
///   un no-SuperAdmin no puede otorgar/retirar el rol de plataforma.
/// </summary>
public sealed class UsersControllerTests : IDisposable
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly UsersController _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Role _employeeRole;
    private readonly Role _superAdminRole;

    public UsersControllerTests()
    {
        var tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(tenantId);
        _tenant.Setup(t => t.BypassTenantFilter).Returns(false);
        _db = new ZorvianDbContext(options, _tenant.Object);

        _employeeRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = RoleType.Employee,
            DisplayName = "Empleado",
            IsSystem = true,
        };
        _superAdminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = RoleType.SuperAdmin,
            DisplayName = "Super Admin",
            IsSystem = true,
        };
        var customRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = RoleType.Custom,
            DisplayName = "Auditor",
            IsSystem = false,
        };

        _db.Roles.AddRange(_employeeRole, _superAdminRole, customRole);
        _db.Users.Add(new User
        {
            Id = _userId,
            TenantId = tenantId,
            FirebaseUid = "uid-1",
            Email = "ana@zorvian.com",
            DisplayName = "Ana",
            IsActive = true,
        });
        _db.SaveChanges();

        _sut = new UsersController(_db, _tenant.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("CompanyAdmin") },
            },
        };
    }

    public void Dispose() => _db.Dispose();

    private static ClaimsPrincipal BuildPrincipal(string role) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role),
        }, "mock"));

    private void ActAs(string role) =>
        _sut.ControllerContext.HttpContext.User = BuildPrincipal(role);

    [Fact]
    public async Task GetRoles_SerializesNameAsString_AndFlagsSystemRoles()
    {
        var result = await _sut.GetRoles();

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        var roles = doc.RootElement.EnumerateArray().ToList();

        Assert.Equal(3, roles.Count);

        var employee = roles.Single(r => r.GetProperty("DisplayName").GetString() == "Empleado");
        Assert.Equal(JsonValueKind.String, employee.GetProperty("Name").ValueKind);
        Assert.Equal("Employee", employee.GetProperty("Name").GetString());
        Assert.True(employee.GetProperty("IsSystem").GetBoolean());

        var custom = roles.Single(r => r.GetProperty("DisplayName").GetString() == "Auditor");
        Assert.Equal("Custom", custom.GetProperty("Name").GetString());
        Assert.False(custom.GetProperty("IsSystem").GetBoolean());

        // Los roles del sistema van primero (el desplegable los agrupa).
        Assert.True(roles[0].GetProperty("IsSystem").GetBoolean());
    }

    [Fact]
    public async Task AssignRole_CustomRoleWithUserManage_CanAssignCompanyRoles()
    {
        // Un rol personalizado con `user.manage` NO puede ser bloqueado por un
        // chequeo hardcodeado de rol: el permiso es la fuente de verdad.
        ActAs(RoleType.Custom.ToString());

        var result = await _sut.AssignRole(_userId, new AssignRoleRequest(_employeeRole.Id));

        Assert.IsType<OkObjectResult>(result);
        var assigned = await _db.UserRoles.Where(ur => ur.UserId == _userId).ToListAsync();
        Assert.Single(assigned);
        Assert.Equal(_employeeRole.Id, assigned[0].RoleId);
    }

    [Fact]
    public async Task AssignRole_CompanyAdmin_CannotGrantPlatformRole()
    {
        ActAs(RoleType.CompanyAdmin.ToString());

        var result = await _sut.AssignRole(_userId, new AssignRoleRequest(_superAdminRole.Id));

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(await _db.UserRoles.Where(ur => ur.UserId == _userId).ToListAsync());
    }

    [Fact]
    public async Task AssignRole_SuperAdmin_CanGrantPlatformRole()
    {
        ActAs(RoleType.SuperAdmin.ToString());

        var result = await _sut.AssignRole(_userId, new AssignRoleRequest(_superAdminRole.Id));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AssignRole_NonSuperAdmin_CannotTouchAUserThatHasSuperAdmin()
    {
        _db.UserRoles.Add(new UserRole { UserId = _userId, RoleId = _superAdminRole.Id });
        _db.SaveChanges();
        ActAs(RoleType.CompanyAdmin.ToString());

        var result = await _sut.AssignRole(_userId, new AssignRoleRequest(_employeeRole.Id));

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AssignRole_ReplacesExistingRoles()
    {
        ActAs(RoleType.SuperAdmin.ToString());
        await _sut.AssignRole(_userId, new AssignRoleRequest(_superAdminRole.Id));

        var result = await _sut.AssignRole(_userId, new AssignRoleRequest(_employeeRole.Id));

        Assert.IsType<OkObjectResult>(result);
        var assigned = await _db.UserRoles.Where(ur => ur.UserId == _userId).ToListAsync();
        Assert.Single(assigned);
        Assert.Equal(_employeeRole.Id, assigned[0].RoleId);
    }
}
