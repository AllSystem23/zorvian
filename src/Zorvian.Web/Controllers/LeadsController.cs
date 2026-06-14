using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/crm/leads")]
public sealed class LeadsController : ControllerBase
{
    private readonly LeadService _service;
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public LeadsController(LeadService service, ZorvianDbContext db, ITenantContext tenant, IMapper mapper)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
        _mapper = mapper;
    }

    [Audit("Lead", "ReadList")]
    [RequirePermission(Permissions.SaleRead)]
    [HttpGet]
    public async Task<IActionResult> GetLeads([FromQuery] LeadFilterRequest filter)
    {
        var (leads, total) = await _service.GetLeadsAsync(filter.Search, filter.Status, filter.Page, filter.PageSize);
        var response = _mapper.Map<List<LeadResponse>>(leads);
        return Ok(new { data = response, total });
    }

    [Audit("Lead", "Read")]
    [RequirePermission(Permissions.SaleRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var lead = await _service.GetLeadByIdAsync(id);
        if (lead == null) return NotFound();
        return Ok(_mapper.Map<LeadResponse>(lead));
    }

    [Audit("Lead", "Create")]
    [RequirePermission(Permissions.SaleWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeadRequest request)
    {
        var lead = _mapper.Map<Lead>(request);
        var created = await _service.CreateLeadAsync(lead);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<LeadResponse>(created));
    }

    [Audit("Lead", "Update")]
    [RequirePermission(Permissions.SaleWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadRequest request)
    {
        var lead = await _service.GetLeadByIdAsync(id);
        if (lead == null) return NotFound();
        
        _mapper.Map(request, lead);
        await _service.UpdateLeadAsync(lead);
        return Ok(_mapper.Map<LeadResponse>(lead));
    }

    [Audit("Lead", "Delete")]
    [RequirePermission(Permissions.SaleWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteLeadAsync(id);
        return NoContent();
    }

    [Audit("Lead", "ReadList")]
    [RequirePermission(Permissions.SaleRead)]
    [HttpGet("{leadId:guid}/activities")]
    public async Task<IActionResult> GetActivities(Guid leadId)
    {
        var activities = await _db.CommercialActivities
            .Include(a => a.CreatedByUser)
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ActivityResponse(
                a.Id,
                a.Type,
                a.Subject ?? string.Empty,
                a.Description,
                a.CreatedByUser != null ? a.CreatedByUser.DisplayName : null,
                a.CreatedAt
            ))
            .ToListAsync();

        return Ok(activities);
    }

    [Audit("Lead", "Create")]
    [RequirePermission(Permissions.SaleWrite)]
    [HttpPost("{leadId:guid}/activities")]
    public async Task<IActionResult> CreateActivity(Guid leadId, [FromBody] CreateActivityRequest request)
    {
        var lead = await _service.GetLeadByIdAsync(leadId);
        if (lead == null) return NotFound();

        var activity = new CommercialActivity
        {
            LeadId = leadId,
            Type = request.Type,
            Subject = request.Subject,
            Description = request.Description,
            Status = "pending",
            CreatedById = _tenant.CurrentUserId ?? Guid.Empty,
            CompanyId = _tenant.TenantId.Value,
        };

        _db.CommercialActivities.Add(activity);
        await _db.SaveChangesAsync();

        var response = new ActivityResponse(
            activity.Id,
            activity.Type,
            activity.Subject ?? string.Empty,
            activity.Description,
            null,
            activity.CreatedAt
        );

        return CreatedAtAction(nameof(GetActivities), new { leadId }, response);
    }
}
