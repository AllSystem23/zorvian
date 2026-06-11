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

    public KpiService(IKpiRepository repo, IMapper mapper, ITenantContext tenant)
    {
        _repo = repo;
        _mapper = mapper;
        _tenant = tenant;
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
        record.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddKpiRecordAsync(record);
        return record;
    }

    public async Task<List<KpiRecord>> GetKpiRecordsAsync(Guid? definitionId, Guid? employeeId, string? periodKey)
    {
        if (definitionId.HasValue)
            return await _repo.GetKpiRecordsByDefinitionIdAsync(definitionId.Value);
        if (employeeId.HasValue)
            return await _repo.GetKpiRecordsByEmployeeIdAsync(employeeId.Value);
        if (!string.IsNullOrEmpty(periodKey))
            return await _repo.GetKpiRecordsByPeriodAsync(periodKey);
        return [];
    }

    public async Task<List<Ranking>> GenerateRankingAsync(string periodKey, string rankingType)
    {
        var records = await _repo.GetKpiRecordsByPeriodAsync(periodKey);
        var ranked = records
            .GroupBy(r => r.EmployeeId)
            .Select(g => new Ranking
            {
                RankingType = rankingType,
                PeriodKey = periodKey,
                EntityId = g.Key ?? Guid.Empty,
                EntityName = g.First().Employee != null ? $"{g.First().Employee!.FirstName} {g.First().Employee!.LastName}" : "Unknown",
                Value = g.Average(r => r.CompliancePercentage),
                TenantId = _tenant.TenantId.Value.ToString(),
            })
            .OrderByDescending(r => r.Value)
            .Select((r, i) =>
            {
                r.Position = i + 1;
                return r;
            })
            .ToList();

        if (ranked.Count > 0)
            await _repo.AddRankingsAsync(ranked);

        return ranked;
    }

    public async Task<List<Ranking>> GetRankingsAsync(string periodKey, string? rankingType)
    {
        if (!string.IsNullOrEmpty(rankingType))
            return await _repo.GetRankingsByTypeAsync(rankingType, periodKey);
        return await _repo.GetRankingsByPeriodAsync(periodKey);
    }
}
