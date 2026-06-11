using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IKpiRepository
{
    Task<List<KpiDefinition>> GetKpiDefinitionsAsync();
    Task<KpiDefinition?> GetKpiDefinitionByIdAsync(Guid id);
    Task AddKpiDefinitionAsync(KpiDefinition definition);
    Task UpdateKpiDefinitionAsync(KpiDefinition definition);
    Task DeleteKpiDefinitionAsync(Guid id);
    Task<List<KpiRecord>> GetKpiRecordsByDefinitionIdAsync(Guid definitionId);
    Task<List<KpiRecord>> GetKpiRecordsByEmployeeIdAsync(Guid employeeId);
    Task<List<KpiRecord>> GetKpiRecordsByPeriodAsync(string periodKey);
    Task AddKpiRecordAsync(KpiRecord record);
    Task AddKpiRecordsAsync(List<KpiRecord> records);
    Task<List<Ranking>> GetRankingsByPeriodAsync(string periodKey);
    Task<List<Ranking>> GetRankingsByTypeAsync(string rankingType, string periodKey);
    Task AddRankingAsync(Ranking ranking);
    Task AddRankingsAsync(List<Ranking> rankings);
}
