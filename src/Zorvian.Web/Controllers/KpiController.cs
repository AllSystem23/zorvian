using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/kpi")]
public sealed class KpiController : ControllerBase
{
    private readonly KpiService _service;

    public KpiController(KpiService service) => _service = service;

    [Audit("Kpi", "ReadDefinitions")]
    [HttpGet("definitions")]
    [RequirePermission(Permissions.KpiRead)]
    public async Task<IActionResult> GetDefinitions() =>
        Ok(await _service.GetKpiDefinitionsAsync());

    [Audit("Kpi", "ReadDefinition")]
    [HttpGet("definitions/{id:guid}")]
    [RequirePermission(Permissions.KpiRead)]
    public async Task<IActionResult> GetDefinitionById(Guid id)
    {
        var result = await _service.GetKpiDefinitionByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Kpi", "CreateDefinition")]
    [HttpPost("definitions")]
    [RequirePermission(Permissions.KpiWrite)]
    public async Task<IActionResult> CreateDefinition([FromBody] KpiDefinition definition)
    {
        var result = await _service.CreateKpiDefinitionAsync(definition);
        return CreatedAtAction(nameof(GetDefinitionById), new { id = result.Id }, result);
    }

    [Audit("Kpi", "UpdateDefinition")]
    [HttpPut("definitions/{id:guid}")]
    [RequirePermission(Permissions.KpiWrite)]
    public async Task<IActionResult> UpdateDefinition(Guid id, [FromBody] KpiDefinition definition)
    {
        var result = await _service.UpdateKpiDefinitionAsync(id, definition);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Kpi", "DeleteDefinition")]
    [HttpDelete("definitions/{id:guid}")]
    [RequirePermission(Permissions.KpiWrite)]
    public async Task<IActionResult> DeleteDefinition(Guid id) =>
        await _service.DeleteKpiDefinitionAsync(id) ? NoContent() : NotFound();

    [Audit("Kpi", "RecordValue")]
    [HttpPost("records")]
    [RequirePermission(Permissions.KpiWrite)]
    public async Task<IActionResult> RecordKpiValue([FromBody] KpiRecord record)
    {
        var result = await _service.RecordKpiValueAsync(record);
        return Ok(result);
    }

    [Audit("Kpi", "ReadRecords")]
    [HttpGet("records")]
    [RequirePermission(Permissions.KpiRead)]
    public async Task<IActionResult> GetRecords([FromQuery] Guid? definitionId, [FromQuery] string? periodKey, [FromQuery] Guid? branchId)
    {
        var result = await _service.GetKpiRecordsAsync(definitionId, null, periodKey, branchId);
        return Ok(result);
    }

    [Audit("Kpi", "GenerateRankings")]
    [HttpPost("rankings/generate")]
    [RequirePermission(Permissions.KpiWrite)]
    public async Task<IActionResult> GenerateRankings([FromBody] GenerateRankingsRequest request)
    {
        var result = await _service.GenerateRankingAsync(request.PeriodKey, request.RankingType, request.BranchId);
        return Ok(result);
    }

    [Audit("Kpi", "ReadRankings")]
    [HttpGet("rankings")]
    [RequirePermission(Permissions.KpiRead)]
    public async Task<IActionResult> GetRankings([FromQuery] string? type, [FromQuery] string? periodKey, [FromQuery] Guid? branchId)
    {
        if (string.IsNullOrEmpty(periodKey))
            return Ok(new List<Ranking>());
        var result = await _service.GetRankingsAsync(periodKey, type, branchId);
        return Ok(result);
    }
}

// BranchId opcional: null = usar la sucursal del contexto (header X-Branch-Id);
// Guid.Empty explícito = rankings/records de TODAS las sucursales.
public sealed record GenerateRankingsRequest(string PeriodKey, string RankingType, Guid? BranchId = null);
