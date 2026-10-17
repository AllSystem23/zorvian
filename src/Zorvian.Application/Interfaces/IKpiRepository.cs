using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IKpiRepository
{
    Task<List<KpiDefinition>> GetKpiDefinitionsAsync();
    Task<KpiDefinition?> GetKpiDefinitionByIdAsync(Guid id);
    Task AddKpiDefinitionAsync(KpiDefinition definition);
    Task UpdateKpiDefinitionAsync(KpiDefinition definition);
    Task DeleteKpiDefinitionAsync(Guid id);
    // Semántica de sucursal (ver BranchScopeExtensions): null o Guid.Empty = todas
    // las sucursales; cualquier otro GUID filtra por igualdad exacta.
    Task<List<KpiRecord>> GetKpiRecordsByDefinitionIdAsync(Guid definitionId, Guid? branchId = null);
    Task<List<KpiRecord>> GetKpiRecordsByEmployeeIdAsync(Guid employeeId, Guid? branchId = null);
    Task<List<KpiRecord>> GetKpiRecordsByPeriodAsync(string periodKey, Guid? branchId = null);
    Task AddKpiRecordAsync(KpiRecord record);
    Task AddKpiRecordsAsync(List<KpiRecord> records);
    Task AddRankingAsync(Ranking ranking);
    Task AddRankingsAsync(List<Ranking> rankings);
    Task<List<Ranking>> GetRankingsByPeriodAsync(string periodKey, Guid? branchId = null);
    Task<List<Ranking>> GetRankingsByTypeAsync(string rankingType, string periodKey, Guid? branchId = null);

    /// <summary>
    /// Elimina los rankings de (periodKey, rankingType) dentro del alcance dado:
    /// branchId null/Empty borra TODOS los del período+tipo (regeneración company-wide);
    /// un GUID concreto borra solo los de esa sucursal (la otra sucursal no se toca).
    /// </summary>
    Task DeleteRankingsAsync(string periodKey, string rankingType, Guid? branchId);
}
