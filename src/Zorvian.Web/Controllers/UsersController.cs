using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

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

    [HttpGet]
    [RequirePermission(Permissions.UserRead)]
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

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.UserRead)]
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

    [Audit("User", "AssignRole")]
    [HttpPut("{id:guid}/role")]
    [RequirePermission(Permissions.UserManage)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        var currentUserRole = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .FirstOrDefault();

        if (currentUserRole != RoleType.SuperAdmin.ToString() && currentUserRole != RoleType.CompanyAdmin.ToString())
            return Forbid();

        var user = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == _tenant.TenantId)
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return NotFound(new { error = "User not found" });

        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && (r.TenantId == _tenant.TenantId || r.IsSystem));

        if (role is null)
            return NotFound(new { error = "Role not found" });

        var targetRoles = await _db.UserRoles
            .IgnoreQueryFilters()
            .Where(ur => ur.UserId == id)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        if (targetRoles.Contains(RoleType.SuperAdmin) && currentUserRole != RoleType.SuperAdmin.ToString())
            return Forbid();

        user.UserRoles.Clear();
        user.UserRoles.Add(new UserRole { UserId = id, RoleId = request.RoleId });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Rol asignado correctamente" });
    }

    [Audit("User", "ToggleActive")]
    [HttpPut("{id:guid}/toggle-active")]
    [RequirePermission(Permissions.UserManage)]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var currentUserRole = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .FirstOrDefault();

        if (currentUserRole != RoleType.SuperAdmin.ToString() && currentUserRole != RoleType.CompanyAdmin.ToString())
            return Forbid();

        var user = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == _tenant.TenantId)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return NotFound(new { error = "User not found" });

        if (currentUserRole == RoleType.CompanyAdmin.ToString())
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (user.Id.ToString() == currentUserId)
                return BadRequest(new { error = "No puedes desactivarte a ti mismo" });

            var otherAdmins = await _db.UserRoles
                .IgnoreQueryFilters()
                .Where(ur => ur.Role.Name == RoleType.CompanyAdmin && ur.User.TenantId == _tenant.TenantId && ur.User.Id != id && ur.User.IsActive)
                .CountAsync();

            if (otherAdmins == 0)
                return BadRequest(new { error = "Debe haber al menos un CompanyAdmin activo" });
        }

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new { isActive = user.IsActive });
    }

    [HttpGet("roles")]
    [RequirePermission(Permissions.UserRead)]
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
