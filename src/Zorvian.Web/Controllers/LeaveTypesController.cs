using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Permission;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de tipos de ausencia. Administra los tipos de permiso o vacación configurados por la empresa.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/leave-types")]
public sealed class LeaveTypesController : ControllerBase
{
    private readonly IPermissionRepository _repo;
    private readonly ITenantContext _tenant;

    public LeaveTypesController(IPermissionRepository repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    /// <summary>
    /// Obtiene todos los tipos de ausencia configurados para la empresa.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var types = await _repo.GetByCompanyAsync(_tenant.TenantId is not null ? null : null);
        return Ok(types.Select(t => new PermissionTypeResponse(
            t.Id, t.Code, t.Name, t.IsPaid, t.RequiresAttachment,
            t.RequiresApproval, t.MaxDaysPerRequest, t.MaxDaysPerMonth,
            t.MaxDaysPerYear, t.Description
        )));
    }

    /// <summary>
    /// Crea un nuevo tipo de ausencia.
    /// </summary>
    [Audit("LeaveType", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveTypeRequest request)
    {
        var leaveType = new LeaveType
        {
            TenantId = _tenant.TenantId ?? "",
            Code = request.Code.ToUpper(),
            Name = request.Name,
            Description = request.Description,
            IsPaid = request.IsPaid,
            RequiresAttachment = request.RequiresAttachment,
            RequiresApproval = request.RequiresApproval,
            MaxDaysPerRequest = request.MaxDaysPerRequest,
            MaxDaysPerMonth = request.MaxDaysPerMonth,
            MaxDaysPerYear = request.MaxDaysPerYear,
            Country = request.Country ?? "NI",
        };

        await _repo.AddLeaveTypeAsync(leaveType);
        await _repo.SaveChangesAsync();

        return CreatedAtAction(null, new { id = leaveType.Id }, new PermissionTypeResponse(
            leaveType.Id, leaveType.Code, leaveType.Name, leaveType.IsPaid,
            leaveType.RequiresAttachment, leaveType.RequiresApproval,
            leaveType.MaxDaysPerRequest, leaveType.MaxDaysPerMonth,
            leaveType.MaxDaysPerYear, leaveType.Description
        ));
    }

    /// <summary>
    /// Actualiza un tipo de ausencia existente.
    /// </summary>
    [Audit("LeaveType", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateLeaveTypeRequest request)
    {
        var leaveType = await _repo.GetLeaveTypeByIdAsync(id);
        if (leaveType is null)
            return NotFound(new { error = "Leave type not found" });

        leaveType.Code = request.Code.ToUpper();
        leaveType.Name = request.Name;
        leaveType.Description = request.Description;
        leaveType.IsPaid = request.IsPaid;
        leaveType.RequiresAttachment = request.RequiresAttachment;
        leaveType.RequiresApproval = request.RequiresApproval;
        leaveType.MaxDaysPerRequest = request.MaxDaysPerRequest;
        leaveType.MaxDaysPerMonth = request.MaxDaysPerMonth;
        leaveType.MaxDaysPerYear = request.MaxDaysPerYear;
        leaveType.Country = request.Country ?? "NI";

        await _repo.UpdateLeaveTypeAsync(leaveType);
        await _repo.SaveChangesAsync();

        return Ok(new PermissionTypeResponse(
            leaveType.Id, leaveType.Code, leaveType.Name, leaveType.IsPaid,
            leaveType.RequiresAttachment, leaveType.RequiresApproval,
            leaveType.MaxDaysPerRequest, leaveType.MaxDaysPerMonth,
            leaveType.MaxDaysPerYear, leaveType.Description
        ));
    }
}
