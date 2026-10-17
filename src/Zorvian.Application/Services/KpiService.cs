using AutoMapper;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class KpiService
{
    private readonly IKpiRepository _repo;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenant;
    private readonly IBranchValidator _branchValidator;
    private readonly IEmployeeRepository _employeeRepo;

    public KpiService(
        IKpiRepository repo,
        IMapper mapper,
        ITenantContext tenant,
        IBranchValidator branchValidator,
        IEmployeeRepository employeeRepo)
    {
        _repo = repo;
        _mapper = mapper;
        _tenant = tenant;
        _branchValidator = branchValidator;
        _employeeRepo = employeeRepo;
    }

    public async Task<List<KpiDefinition>> GetKpiDefinitionsAsync() =>
        await _repo.GetKpiDefinitionsAsync();

    public async Task<KpiDefinition?> GetKpiDefinitionByIdAsync(Guid id) =>
        await _repo.GetKpiDefinitionByIdAsync(id);

    public async Task<KpiDefinition> CreateKpiDefinitionAsync(KpiDefinition definition)
    {
        definition.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddKpiDefinitionAsync(definition);
        return definition;
    }

    public async Task<KpiDefinition?> UpdateKpiDefinitionAsync(Guid id, KpiDefinition definition)
    {
        var existing = await _repo.GetKpiDefinitionByIdAsync(id);
        if (existing is null) return null;
        _mapper.Map(definition, existing);
        await _repo.UpdateKpiDefinitionAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteKpiDefinitionAsync(Guid id)
    {
        var existing = await _repo.GetKpiDefinitionByIdAsync(id);
        if (existing is null) return false;
        await _repo.DeleteKpiDefinitionAsync(id);
        return true;
    }

    public async Task<KpiRecord> RecordKpiValueAsync(KpiRecord record)
    {
        var definition = await _repo.GetKpiDefinitionByIdAsync(record.KpiDefinitionId);
        if (definition is not null)
        {
            record.TargetValue ??= definition.TargetValue;
            if (record.TargetValue > 0)
            {
                record.CompliancePercentage = Math.Round((record.ActualValue / record.TargetValue.Value) * 100, 2);
            }
        }

        // Sucursal del registro (dimensión de reporte): explícita VALIDADA contra
        // la compañía (lanza si es cross-company) → sucursal del empleado (donde
        // trabaja, no donde navega quien lo registra) → sucursal del contexto.
        if (record.BranchId is { } explicitBranch && explicitBranch != Guid.Empty)
        {
            record.BranchId = await _branchValidator.ResolveForWriteAsync(explicitBranch);
        }
        else if (record.EmployeeId is { } employeeId &&
                 await _employeeRepo.GetByIdAsync(employeeId) is { BranchId: { } employeeBranch })
        {
            record.BranchId = employeeBranch;
        }
        else
        {
            record.BranchId = _tenant.ResolveBranchId();
        }

        record.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddKpiRecordAsync(record);
        return record;
    }

    public async Task<List<KpiRecord>> GetKpiRecordsAsync(Guid? definitionId, Guid? employeeId, string? periodKey, Guid? branchId = null)
    {
        // Filtro explícito ?? sucursal del contexto ?? todas (Guid.Empty).
        var scope = branchId ?? _tenant.ResolveBranchId() ?? Guid.Empty;
        if (definitionId.HasValue)
            return await _repo.GetKpiRecordsByDefinitionIdAsync(definitionId.Value, scope);
        if (employeeId.HasValue)
            return await _repo.GetKpiRecordsByEmployeeIdAsync(employeeId.Value, scope);
        if (!string.IsNullOrEmpty(periodKey))
            return await _repo.GetKpiRecordsByPeriodAsync(periodKey, scope);
        return [];
    }

    public async Task<List<Ranking>> GenerateRankingAsync(string periodKey, string rankingType, Guid? branchId = null)
    {
        var scope = branchId ?? _tenant.ResolveBranchId() ?? Guid.Empty;
        var records = await _repo.GetKpiRecordsByPeriodAsync(periodKey, scope);

        // Regeneración idempotente: reemplaza SOLO el alcance procesado. Antes
        // cada POST acumulaba filas nuevas y duplicaba posiciones en el período.
        await _repo.DeleteRankingsAsync(periodKey, rankingType, scope);

        var tenantId = _tenant.TenantId.Value.ToString();
        var groups = records
            .GroupBy(r => r.EmployeeId)
            .Select(g =>
            {
                // Sucursal del renglón: con alcance concreto manda el alcance (los
                // records ya vienen filtrados); con "todas" se deriva por empleado
                // (y, en su ausencia, por la sucursal de sus records).
                var branch = scope != Guid.Empty
                    ? scope
                    : g.FirstOrDefault()?.Employee?.BranchId
                      ?? g.Select(r => r.BranchId).FirstOrDefault(b => b is { } bb && bb != Guid.Empty);

                return new Ranking
                {
                    RankingType = rankingType,
                    PeriodKey = periodKey,
                    EntityId = g.Key ?? Guid.Empty,
                    EntityName = g.First().Employee != null ? $"{g.First().Employee!.FirstName} {g.First().Employee!.LastName}" : "Unknown",
                    Value = g.Average(r => r.CompliancePercentage),
                    BranchId = branch,
                    TenantId = tenantId,
                };
            })
            .ToList();

        // Posiciones DENTRO de cada sucursal (cada branch arranca en 1): con
        // "todas" una sola corrida genera un ranking independiente por sucursal.
        var ranked = groups
            .GroupBy(r => r.BranchId)
            .SelectMany(bucket => bucket
                .OrderByDescending(r => r.Value)
                .ThenBy(r => r.EntityName)
                .Select((r, i) =>
                {
                    r.Position = i + 1;
                    return r;
                }))
            .OrderBy(r => r.BranchId)
            .ThenBy(r => r.Position)
            .ToList();

        if (ranked.Count > 0)
            await _repo.AddRankingsAsync(ranked);

        return ranked;
    }

    public async Task<List<Ranking>> GetRankingsAsync(string periodKey, string? rankingType, Guid? branchId = null)
    {
        var scope = branchId ?? _tenant.ResolveBranchId() ?? Guid.Empty;
        if (!string.IsNullOrEmpty(rankingType))
            return await _repo.GetRankingsByTypeAsync(rankingType, periodKey, scope);
        return await _repo.GetRankingsByPeriodAsync(periodKey, scope);
    }
}
