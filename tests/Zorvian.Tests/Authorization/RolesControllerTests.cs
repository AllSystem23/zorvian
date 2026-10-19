using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Authorization;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Authorization;
using Zorvian.Web.Controllers;

namespace Zorvian.Tests.Authorization;

/// <summary>
/// Matriz rol→permisos: catálogo, lectura por compañía y guardas de escritura
/// (SuperAdmin-only, códigos desconocidos, rol SuperAdmin intocable, tenant).
/// </summary>
public sealed class RolesControllerTests : IDisposable
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly RolesController _sut;
    private readonly string _tenantId;
    private readonly Role _companyAdminRole;
    private readonly Role _employeeRole;

    public RolesControllerTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _tenant.Setup(t => t.BypassTenantFilter).Returns(false);
        _db = new ZorvianDbContext(options, _tenant.Object);

        _companyAdminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = RoleType.CompanyAdmin,
            DisplayName = "Admin",
            IsSystem = true,
            RolePermissions = new List<RolePermission>
            {
                new() { PermissionCode = "user.read" },
                new() { PermissionCode = "user.manage" },
            },
        };
        _employeeRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = RoleType.Employee,
            DisplayName = "Empleado",
            IsSystem = true,
            RolePermissions = new List<RolePermission>(),
        };
        var superAdminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = RoleType.SuperAdmin,
            DisplayName = "Super Admin",
            IsSystem = true,
            RolePermissions = new List<RolePermission>(),
        };

        _db.Roles.AddRange(_companyAdminRole, _employeeRole, superAdminRole);
        // Rol de OTRA compañía: nunca debe ser visible/editable.
        _db.Roles.Add(new Role
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid().ToString(),
            Name = RoleType.Employee,
            DisplayName = "Empleado Ajeno",
            IsSystem = true,
        });
        _db.SaveChanges();

        _sut = new RolesController(_db, _tenant.Object)
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

    private void ActAsSuperAdmin() =>
        _sut.ControllerContext.HttpContext.User = BuildPrincipal("SuperAdmin");

    [Fact]
    public void Catalog_CoversEveryPermissionConstant_WithoutDuplicates()
    {
        var constantCodes = typeof(Permissions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.Equal(constantCodes.Count, PermissionCatalog.All.Count);
        Assert.Equal(constantCodes.OrderBy(c => c), PermissionCatalog.Codes.OrderBy(c => c));
        Assert.Equal(PermissionCatalog.All.Count, PermissionCatalog.Codes.Distinct().Count());
    }

    [Fact]
    public void Catalog_MarksSuperAdminOnlyPermissions()
    {
        var privileged = PermissionCatalog.All.Where(p => p.IsSuperAdminOnly).Select(p => p.Code).ToList();

        Assert.Equal(new[] { "apikey.manage", "company.manage", "role.manage" }, privileged.OrderBy(c => c));
        Assert.All(PermissionCatalog.All, p => Assert.False(string.IsNullOrWhiteSpace(p.Label)));
        Assert.All(PermissionCatalog.All, p => Assert.False(string.IsNullOrWhiteSpace(p.Group)));
    }

    [Fact]
    public async Task GetRoles_ReturnsCompanyRolesWithPermissions()
    {
        var result = await _sut.GetRoles();

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var roles = doc.RootElement.EnumerateArray().ToList();

        // Solo los 3 roles de ESTA compañía (el ajeno no aparece).
        Assert.Equal(3, roles.Count);
        // Ojo: `Name` (enum) se serializa como número; usamos DisplayName.
        var admin = roles.Single(r => r.GetProperty("DisplayName").GetString() == "Admin");
        var perms = admin.GetProperty("Permissions").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Contains("user.read", perms);
        Assert.Contains("user.manage", perms);
    }

    [Fact]
    public async Task UpdatePermissions_ReplacesSet()
    {
        var result = await _sut.UpdatePermissions(_employeeRole.Id, new UpdateRolePermissionsRequest(
            new List<string> { "sale.read", "client.read", "sale.read" }));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var perms = doc.RootElement.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToList();

        // Distinct aplicado.
        Assert.Equal(new[] { "client.read", "sale.read" }, perms.OrderBy(p => p));
        Assert.Contains("Permisos actualizados", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task UpdatePermissions_UnknownCode_ReturnsBadRequest()
    {
        var result = await _sut.UpdatePermissions(_employeeRole.Id, new UpdateRolePermissionsRequest(
            new List<string> { "sale.read", "no.existe" }));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("no reconocidos", bad.Value!.ToString());
    }

    [Fact]
    public async Task UpdatePermissions_SuperAdminRole_ReturnsBadRequest()
    {
        var superAdminRole = _db.Roles.Single(r => r.Name == RoleType.SuperAdmin && r.TenantId == _tenantId);

        var result = await _sut.UpdatePermissions(superAdminRole.Id, new UpdateRolePermissionsRequest(
            new List<string> { "sale.read" }));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("SuperAdmin", bad.Value!.ToString());
    }

    [Fact]
    public async Task UpdatePermissions_CompanyAdmin_CannotGrantPrivilegedPermissions()
    {
        var result = await _sut.UpdatePermissions(_employeeRole.Id, new UpdateRolePermissionsRequest(
            new List<string> { "sale.read", "apikey.manage" }));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Solo SuperAdmin", bad.Value!.ToString());
    }

    [Fact]
    public async Task UpdatePermissions_SuperAdmin_CanGrantPrivilegedPermissions()
    {
        ActAsSuperAdmin();

        var result = await _sut.UpdatePermissions(_employeeRole.Id, new UpdateRolePermissionsRequest(
            new List<string> { "role.manage" }));

        Assert.IsType<OkObjectResult>(result);
        Assert.Contains(_db.RolePermissions.Where(rp => rp.RoleId == _employeeRole.Id),
            rp => rp.PermissionCode == "role.manage");
    }

    [Fact]
    public async Task UpdatePermissions_RoleFromAnotherCompany_ReturnsNotFound()
    {
        var foreignRoleId = _db.Roles.Single(r => r.TenantId != _tenantId).Id;

        var result = await _sut.UpdatePermissions(foreignRoleId, new UpdateRolePermissionsRequest(
            new List<string> { "sale.read" }));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Quién entra lo decide el permiso granular `role.manage` (CompanyAdmin lo
    /// trae sembrado), no un guard por rol dentro del método: los atributos son
    /// metadata de filtro y no se ejecutan al invocar el método directo, así que
    /// aquí se verifica su presencia.
    /// </summary>
    [Theory]
    [InlineData(nameof(RolesController.GetPermissionsCatalog))]
    [InlineData(nameof(RolesController.GetRoles))]
    [InlineData(nameof(RolesController.UpdatePermissions))]
    public void Surface_RequiresRoleManagePermission(string methodName)
    {
        var method = typeof(RolesController).GetMethod(methodName);

        var attribute = method!.GetCustomAttributes<RequirePermissionAttribute>().SingleOrDefault();

        Assert.True(attribute is not null, $"{methodName} debería exigir [RequirePermission]");
        Assert.Equal(Permissions.RoleManage, attribute!.Permission);
    }

    // ── CRUD de roles personalizados ──

    [Fact]
    public async Task GetRoles_SerializesNameAsString()
    {
        var result = await _sut.GetRoles();

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var roles = doc.RootElement.EnumerateArray().ToList();

        // Regresión: `Name` (enum) viajaba como NÚMERO y el frontend lo casteaba
        // a String → el parseo de la matriz de roles reventaba en producción.
        Assert.All(roles, r =>
            Assert.Equal(System.Text.Json.JsonValueKind.String, r.GetProperty("Name").ValueKind));

        var admin = roles.Single(r => r.GetProperty("DisplayName").GetString() == "Admin");
        Assert.Equal("CompanyAdmin", admin.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task CreateRole_CreatesCustomRoleWithPermissions()
    {
        var result = await _sut.CreateRole(new CreateRoleRequest(
            "Auditor de Ventas", "Solo lectura comercial",
            new List<string> { "sale.read", "client.read", "sale.read" }));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(created.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("Custom", doc.RootElement.GetProperty("Name").GetString());
        Assert.False(doc.RootElement.GetProperty("IsSystem").GetBoolean());
        Assert.Equal("Auditor de Ventas", doc.RootElement.GetProperty("DisplayName").GetString());

        var role = _db.Roles.Single(r => r.DisplayName == "Auditor de Ventas");
        Assert.Equal(RoleType.Custom, role.Name);
        Assert.Equal(_tenantId, role.TenantId);
        Assert.Equal(
            new[] { "client.read", "sale.read" },
            _db.RolePermissions.Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionCode).OrderBy(c => c).ToList());
    }

    [Fact]
    public async Task CreateRole_EmptyName_ReturnsBadRequest()
    {
        var result = await _sut.CreateRole(new CreateRoleRequest("   ", null, new List<string>()));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("obligatorio", bad.Value!.ToString());
    }

    [Fact]
    public async Task CreateRole_DuplicateName_ReturnsBadRequest()
    {
        var result = await _sut.CreateRole(new CreateRoleRequest("admin", null, new List<string>()));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Ya existe", bad.Value!.ToString());
    }

    [Fact]
    public async Task CreateRole_UnknownPermission_ReturnsBadRequest()
    {
        var result = await _sut.CreateRole(new CreateRoleRequest(
            "Raro", null, new List<string> { "sale.read", "no.existe" }));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("no reconocidos", bad.Value!.ToString());
    }

    [Fact]
    public async Task CreateRole_CompanyAdmin_CannotGrantPrivilegedPermission()
    {
        var result = await _sut.CreateRole(new CreateRoleRequest(
            "Admin Lite", null, new List<string> { "role.manage" }));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Solo SuperAdmin", bad.Value!.ToString());
    }

    [Fact]
    public async Task CreateRole_SuperAdmin_CanGrantPrivilegedPermission()
    {
        ActAsSuperAdmin();

        var result = await _sut.CreateRole(new CreateRoleRequest(
            "Admin Lite", null, new List<string> { "role.manage" }));

        Assert.IsType<CreatedAtActionResult>(result);
        var role = _db.Roles.Single(r => r.DisplayName == "Admin Lite");
        Assert.Contains(_db.RolePermissions.Where(rp => rp.RoleId == role.Id),
            rp => rp.PermissionCode == "role.manage");
    }

    [Fact]
    public async Task UpdateRole_RenamesCustomRole()
    {
        await _sut.CreateRole(new CreateRoleRequest("Temporal 2", null, new List<string>()));
        var role2Id = _db.Roles.Single(r => r.DisplayName == "Temporal 2").Id;

        var result = await _sut.UpdateRole(role2Id, new UpdateRoleRequest("Renombrado", "nueva desc"));

        Assert.IsType<OkObjectResult>(result);
        var role = _db.Roles.Single(r => r.Id == role2Id);
        Assert.Equal("Renombrado", role.DisplayName);
        Assert.Equal("nueva desc", role.Description);
    }

    [Fact]
    public async Task UpdateRole_SystemRole_ReturnsBadRequest()
    {
        var result = await _sut.UpdateRole(_employeeRole.Id, new UpdateRoleRequest("Otro nombre", null));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("roles del sistema", bad.Value!.ToString());
    }

    [Fact]
    public async Task UpdateRole_FromAnotherCompany_ReturnsNotFound()
    {
        var foreignRoleId = _db.Roles.Single(r => r.TenantId != _tenantId).Id;

        var result = await _sut.UpdateRole(foreignRoleId, new UpdateRoleRequest("X", null));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRole_RemovesCustomRoleAndItsPermissions()
    {
        await _sut.CreateRole(new CreateRoleRequest("Efímero", null, new List<string> { "sale.read" }));
        var role = _db.Roles.Single(r => r.DisplayName == "Efímero");

        var result = await _sut.DeleteRole(role.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.DoesNotContain(_db.Roles, r => r.Id == role.Id);
        Assert.DoesNotContain(_db.RolePermissions, rp => rp.RoleId == role.Id);
    }

    [Fact]
    public async Task DeleteRole_SystemRole_ReturnsBadRequest()
    {
        var result = await _sut.DeleteRole(_employeeRole.Id);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("roles del sistema", bad.Value!.ToString());
    }

    [Fact]
    public async Task DeleteRole_WithAssignedUsers_ReturnsBadRequest()
    {
        await _sut.CreateRole(new CreateRoleRequest("En Uso", null, new List<string>()));
        var role = _db.Roles.Single(r => r.DisplayName == "En Uso");
        _db.UserRoles.Add(new UserRole { UserId = Guid.NewGuid(), RoleId = role.Id });
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteRole(role.Id);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("usuarios asignados", bad.Value!.ToString());
        Assert.Contains(_db.Roles, r => r.Id == role.Id);
    }

    [Fact]
    public async Task DeleteRole_FromAnotherCompany_ReturnsNotFound()
    {
        var foreignRoleId = _db.Roles.Single(r => r.TenantId != _tenantId).Id;

        var result = await _sut.DeleteRole(foreignRoleId);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
