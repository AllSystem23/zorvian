using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICommissionRepository
{
    Task<List<CommissionScheme>> GetSchemesAsync();
    Task<CommissionScheme?> GetSchemeByIdAsync(Guid id);
    Task AddSchemeAsync(CommissionScheme scheme);
    Task UpdateSchemeAsync(CommissionScheme scheme);
    Task DeleteSchemeAsync(Guid id);
    Task<List<CommissionRule>> GetRulesBySchemeIdAsync(Guid schemeId);
    Task AddRuleAsync(CommissionRule rule);
    Task UpdateRuleAsync(CommissionRule rule);
    Task DeleteRuleAsync(Guid ruleId);
    Task<List<CommissionAssignment>> GetAssignmentsBySchemeIdAsync(Guid schemeId);
    Task<List<CommissionAssignment>> GetAssignmentsByEmployeeIdAsync(Guid employeeId);
    Task AddAssignmentAsync(CommissionAssignment assignment);
    Task UpdateAssignmentAsync(CommissionAssignment assignment);
    Task<List<CommissionRecord>> GetRecordsByPeriodAsync(Guid periodId, Guid companyId);
    Task<List<CommissionRecord>> GetRecordsByEmployeeAsync(Guid employeeId);
    Task<List<CommissionRecord>> GetRecordsBySaleIdAsync(Guid saleId);
    Task<CommissionRecord?> GetRecordByIdAsync(Guid id);
    Task AddRecordAsync(CommissionRecord record);
    Task AddRecordsAsync(List<CommissionRecord> records);
    Task UpdateRecordAsync(CommissionRecord record);
    Task SaveChangesAsync();
}
