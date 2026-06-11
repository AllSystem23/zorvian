using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class KpiRepository : IKpiRepository
{
    private readonly ZorvianDbContext _db;

    public KpiRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<KpiDefinition>> GetKpiDefinitionsAsync() =>
        await _db.KpiDefinitions
            .OrderBy(k => k.Name)
            .ToListAsync();

    public async Task<KpiDefinition?> GetKpiDefinitionByIdAsync(Guid id) =>
        await _db.KpiDefinitions
            .Include(k => k.Records)
            .FirstOrDefaultAsync(k => k.Id == id);

    public async Task AddKpiDefinitionAsync(KpiDefinition definition) =>
        await _db.KpiDefinitions.AddAsync(definition);

    public Task UpdateKpiDefinitionAsync(KpiDefinition definition)
    {
        _db.KpiDefinitions.Update(definition);
        return Task.CompletedTask;
    }

    public async Task DeleteKpiDefinitionAsync(Guid id)
    {
        var kpi = await _db.KpiDefinitions.FindAsync(id);
        if (kpi is not null)
            _db.KpiDefinitions.Remove(kpi);
    }

    public async Task<List<KpiRecord>> GetKpiRecordsByDefinitionIdAsync(Guid definitionId) =>
        await _db.KpiRecords
            .Include(r => r.Employee)
            .Where(r => r.KpiDefinitionId == definitionId)
            .OrderByDescending(r => r.EvaluationDate)
            .ToListAsync();

    public async Task<List<KpiRecord>> GetKpiRecordsByEmployeeIdAsync(Guid employeeId) =>
        await _db.KpiRecords
            .Include(r => r.KpiDefinition)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.EvaluationDate)
            .ToListAsync();

    public async Task<List<KpiRecord>> GetKpiRecordsByPeriodAsync(string periodKey) =>
        await _db.KpiRecords
            .Include(r => r.KpiDefinition)
            .Include(r => r.Employee)
            .Where(r => r.PeriodKey == periodKey)
            .ToListAsync();

    public async Task AddKpiRecordAsync(KpiRecord record) =>
        await _db.KpiRecords.AddAsync(record);

    public async Task AddKpiRecordsAsync(List<KpiRecord> records) =>
        await _db.KpiRecords.AddRangeAsync(records);

    public async Task<List<Ranking>> GetRankingsByPeriodAsync(string periodKey) =>
        await _db.Rankings
            .Where(r => r.PeriodKey == periodKey)
            .OrderBy(r => r.Position)
            .ToListAsync();

    public async Task<List<Ranking>> GetRankingsByTypeAsync(string rankingType, string periodKey) =>
        await _db.Rankings
            .Where(r => r.RankingType == rankingType && r.PeriodKey == periodKey)
            .OrderBy(r => r.Position)
            .ToListAsync();

    public async Task AddRankingAsync(Ranking ranking) =>
        await _db.Rankings.AddAsync(ranking);

    public async Task AddRankingsAsync(List<Ranking> rankings) =>
        await _db.Rankings.AddRangeAsync(rankings);
}
