using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/crm/pipeline-stages")]
public sealed class PipelineStagesController : ControllerBase
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public PipelineStagesController(ZorvianDbContext db, ITenantContext tenantContext, IMapper mapper)
    {
        _db = db;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    [Audit("PipelineStage", "ReadList")]
    [HttpGet]
    public async Task<IActionResult> GetStages()
    {
        var companyId = _tenantContext.TenantId.Value;
        var stages = await _db.PipelineStages
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.Order)
            .ToListAsync();
        
        if (!stages.Any())
        {
            // Seed default stages if none exist for this company
            stages = await SeedDefaultStages(companyId);
        }

        return Ok(_mapper.Map<List<PipelineStageResponse>>(stages));
    }

    private async Task<List<PipelineStage>> SeedDefaultStages(Guid companyId)
    {
        var defaults = new List<PipelineStage>
        {
            new() { Name = "Prospecto", Order = 1, Color = "#9E9E9E", CompanyId = companyId },
            new() { Name = "Contacto", Order = 2, Color = "#2196F3", CompanyId = companyId },
            new() { Name = "Calificado", Order = 3, Color = "#FF9800", CompanyId = companyId },
            new() { Name = "Propuesta", Order = 4, Color = "#9C27B0", CompanyId = companyId },
            new() { Name = "Negociación", Order = 5, Color = "#FFEB3B", CompanyId = companyId },
            new() { Name = "Cerrado Ganado", Order = 6, Color = "#4CAF50", CompanyId = companyId },
            new() { Name = "Cerrado Perdido", Order = 7, Color = "#F44336", CompanyId = companyId }
        };

        await _db.PipelineStages.AddRangeAsync(defaults);
        await _db.SaveChangesAsync();
        return defaults;
    }
}
