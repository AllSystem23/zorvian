using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de usuarios del sistema. Administra consulta, asignación de roles y estado de cuentas de usuario.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public UsersController(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>
    /// Obtiene la lista de usuarios del sistema con sus roles y empleado asociado.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var users = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == _tenant.TenantId && !u.IsDeleted)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Employee)
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        return Ok(users.Select(u => new
        {
            u.Id,
            u.DisplayName,
            u.Email,
            u.IsActive,
            u.LastLoginAt,
            Roles = u.UserRoles.Select(ur => ur.Role.DisplayName).ToList(),
            EmployeeName = u.Employee is not null ? $"{u.Employee.FirstName} {u.Employee.LastName}" : null,
            u.EmployeeId,
        }));
    }

    /// <summary>
    /// Obtiene un usuario del sistema por su identificador.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == _tenant.TenantId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return NotFound(new { error = "User not found" });

        return Ok(new
        {
            user.Id,
            user.DisplayName,
            user.Email,
            user.IsActive,
            user.LastLoginAt,
            user.FirebaseUid,
            Roles = user.UserRoles.Select(ur => new { ur.RoleId, ur.Role.DisplayName, RoleName = ur.Role.Name.ToString() }).ToList(),
            EmployeeName = user.Employee is not null ? $"{user.Employee.FirstName} {user.Employee.LastName}" : null,
            user.EmployeeId,
        });
    }

    /// <summary>
    /// Asigna un rol a un usuario del sistema.
    /// </summary>
    [Audit("User", "AssignRole")]
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == _tenant.TenantId)
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return NotFound(new { error = "User not found" });

        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.TenantId == _tenant.TenantId);

        if (role is null)
            return NotFound(new { error = "Role not found" });

        user.UserRoles.Clear();
        user.UserRoles.Add(new UserRole { UserId = id, RoleId = request.RoleId });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Rol asignado correctamente" });
    }

    /// <summary>
    /// Activa o desactiva un usuario del sistema.
    /// </summary>
    [Audit("User", "ToggleActive")]
    [HttpPut("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == _tenant.TenantId)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return NotFound(new { error = "User not found" });

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new { isActive = user.IsActive });
    }

    /// <summary>
    /// Obtiene los roles disponibles en el sistema.
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.Roles
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == _tenant.TenantId)
            .OrderBy(r => r.DisplayName)
            .ToListAsync();

        return Ok(roles.Select(r => new
        {
            r.Id,
            r.DisplayName,
            r.Name,
            r.Description,
        }));
    }
}

public sealed record AssignRoleRequest(Guid RoleId);
