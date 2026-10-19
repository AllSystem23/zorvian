using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Authorization;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Matriz de permisos granulares por rol de la compañía + CRUD de roles
/// personalizados (crear, renombrar, eliminar).
/// Toda la superficie exige el permiso `role.manage` (CompanyAdmin lo trae
/// sembrado; solo SuperAdmin puede otorgarlo/revocarlo). Otorgar/revocar los
/// permisos de plataforma (`company.manage`, `role.manage`, `apikey.manage`)
/// queda reservado a SuperAdmin.
/// Los roles del sistema (sembrados) NO se renombran ni se eliminan: solo se les
/// ajustan los permisos. Los roles personalizados se guardan con
/// `RoleType.Custom` y se distinguen por `Id`/`DisplayName`.
/// Nota: los permisos viajan como claims del JWT, por lo que los cambios
/// surten efecto en el frontend al refrescar el token (login/refresh), no
/// de forma inmediata en sesiones abiertas.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public RolesController(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>Catálogo de permisos disponibles (código, etiqueta, grupo).</summary>
    [Audit("Role", "ReadPermissionCatalog")]
    [HttpGet("permissions-catalog")]
    [RequirePermission(Permissions.RoleManage)]
    public IActionResult GetPermissionsCatalog() =>
        Ok(PermissionCatalog.All.Select(p => new
        {
            p.Code,
            p.Label,
            p.Group,
            p.IsSuperAdminOnly,
        }));

    /// <summary>Roles de la compañía con sus permisos asignados.</summary>
    [Audit("Role", "ReadList")]
    [HttpGet]
    [RequirePermission(Permissions.RoleManage)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.Roles
            .Where(r => r.TenantId == _tenant.TenantId || _tenant.BypassTenantFilter)
            .Include(r => r.RolePermissions)
            .OrderBy(r => r.DisplayName)
            .ToListAsync();

        return Ok(roles.Select(RoleResponse));
    }

    /// <summary>
    /// Crea un rol personalizado de la compañía con sus permisos.
    /// </summary>
    [Audit("Role", "Create")]
    [HttpPost]
    [RequirePermission(Permissions.RoleManage)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var displayName = (request.DisplayName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            return BadRequest(new { error = "El nombre del rol es obligatorio" });
        if (displayName.Length > 100)
            return BadRequest(new { error = "El nombre del rol no puede superar los 100 caracteres" });

        var requested = NormalizePermissions(request.Permissions);
        var permissionsError = ValidatePermissions(requested, IsSuperAdminCaller());
        if (permissionsError is not null)
            return BadRequest(new { error = permissionsError });

        if (await RoleNameExistsAsync(displayName, exceptId: null))
            return BadRequest(new { error = $"Ya existe un rol llamado \"{displayName}\" en esta compañía" });

        var role = new Core.Entities.Role
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            Name = RoleType.Custom,
            DisplayName = displayName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description!.Trim(),
            IsSystem = false,
        };

        foreach (var code in requested)
            role.RolePermissions.Add(new Core.Entities.RolePermission { RoleId = role.Id, PermissionCode = code });

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoles), null, RoleResponse(role));
    }

    /// <summary>Renombra / describe un rol personalizado de la compañía.</summary>
    [Audit("Role", "Update")]
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.RoleManage)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var role = await FindRoleAsync(id);
        if (role is null)
            return NotFound(new { error = "Rol no encontrado" });

        if (role.IsSystem)
            return BadRequest(new { error = "Los roles del sistema no se pueden renombrar" });

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            var displayName = request.DisplayName!.Trim();
            if (displayName.Length > 100)
                return BadRequest(new { error = "El nombre del rol no puede superar los 100 caracteres" });
            if (await RoleNameExistsAsync(displayName, exceptId: role.Id))
                return BadRequest(new { error = $"Ya existe un rol llamado \"{displayName}\" en esta compañía" });
            role.DisplayName = displayName;
        }

        if (request.Description is not null)
            role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await _db.SaveChangesAsync();

        return Ok(new
        {
            role.Id,
            role.DisplayName,
            role.Description,
            message = "Rol actualizado.",
        });
    }

    /// <summary>
    /// Elimina un rol personalizado. No permite eliminar roles del sistema ni
    /// roles con usuarios asignados (habría que reasignarlos primero).
    /// </summary>
    [Audit("Role", "Delete")]
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.RoleManage)]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id &&
                (r.TenantId == _tenant.TenantId || _tenant.BypassTenantFilter));

        if (role is null)
            return NotFound(new { error = "Rol no encontrado" });

        if (role.IsSystem)
            return BadRequest(new { error = "Los roles del sistema no se pueden eliminar" });

        if (await _db.UserRoles.AnyAsync(ur => ur.RoleId == role.Id))
            return BadRequest(new { error = "El rol tiene usuarios asignados. Reasígnelos antes de eliminarlo." });

        _db.RolePermissions.RemoveRange(role.RolePermissions);
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Reemplaza el conjunto de permisos de un rol de la compañía.
    /// </summary>
    [Audit("Role", "UpdatePermissions")]
    [HttpPut("{id:guid}/permissions")]
    [RequirePermission(Permissions.RoleManage)]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request)
    {
        // `role.manage` ya acota quién entra (SuperAdmin bypassa el claim);
        // `isSuperAdminCaller` solo decide si además puede otorgar los
        // permisos de plataforma.
        var isSuperAdminCaller = IsSuperAdminCaller();

        // Scope estricto por tenant (NO usar el query filter: los roles IsSystem
        // de OTRAS compañías también pasarían por él).
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id && (r.TenantId == _tenant.TenantId || _tenant.BypassTenantFilter));

        if (role is null)
            return NotFound(new { error = "Rol no encontrado" });

        if (role.Name == RoleType.SuperAdmin)
            return BadRequest(new { error = "El rol SuperAdmin siempre tiene todos los permisos y no se puede modificar" });

        var requested = NormalizePermissions(request.Permissions);
        var permissionsError = ValidatePermissions(requested, isSuperAdminCaller);
        if (permissionsError is not null)
            return BadRequest(new { error = permissionsError });

        role.RolePermissions.Clear();
        foreach (var code in requested)
            role.RolePermissions.Add(new Core.Entities.RolePermission { RoleId = role.Id, PermissionCode = code });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            roleId = role.Id,
            permissions = role.RolePermissions.Select(rp => rp.PermissionCode).ToList(),
            message = "Permisos actualizados. Los usuarios con este rol verán los cambios al refrescar su sesión.",
        });
    }

    private bool IsSuperAdminCaller() => User.IsInRole(RoleType.SuperAdmin.ToString());

    private Task<Core.Entities.Role?> FindRoleAsync(Guid id) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Id == id &&
            (r.TenantId == _tenant.TenantId || _tenant.BypassTenantFilter));

    private Task<bool> RoleNameExistsAsync(string displayName, Guid? exceptId)
    {
        var normalized = displayName.ToLower();
        return _db.Roles.AnyAsync(r =>
            r.TenantId == _tenant.TenantId &&
            !r.IsDeleted &&
            (exceptId == null || r.Id != exceptId) &&
            r.DisplayName.ToLower() == normalized);
    }

    private static List<string> NormalizePermissions(IEnumerable<string>? permissions) =>
        (permissions ?? Enumerable.Empty<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct()
            .ToList();

    private static string? ValidatePermissions(List<string> requested, bool isSuperAdminCaller)
    {
        var unknown = requested.Except(PermissionCatalog.Codes).ToList();
        if (unknown.Count > 0)
            return $"Permisos no reconocidos: {string.Join(", ", unknown)}";

        if (!isSuperAdminCaller)
        {
            var privileged = requested.Where(p => PermissionCatalog.SuperAdminOnly.Contains(p)).ToList();
            if (privileged.Count > 0)
                return $"Solo SuperAdmin puede otorgar estos permisos: {string.Join(", ", privileged)}";
        }

        return null;
    }

    private static object RoleResponse(Core.Entities.Role r) => new
    {
        r.Id,
        Name = r.Name.ToString(),
        r.DisplayName,
        r.Description,
        r.IsSystem,
        Permissions = r.RolePermissions.Select(rp => rp.PermissionCode).ToList(),
    };
}

public sealed record UpdateRolePermissionsRequest(List<string>? Permissions);

public sealed record CreateRoleRequest(string? DisplayName, string? Description, List<string>? Permissions);

public sealed record UpdateRoleRequest(string? DisplayName, string? Description);
