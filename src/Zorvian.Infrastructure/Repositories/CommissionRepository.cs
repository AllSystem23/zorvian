using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CommissionRepository : ICommissionRepository
{
    private readonly ZorvianDbContext _db;

    public CommissionRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<CommissionScheme>> GetSchemesAsync() =>
        await _db.CommissionSchemes
            .Include(s => s.Rules)
            .OrderBy(s => s.Name)
            .ToListAsync();

    public async Task<CommissionScheme?> GetSchemeByIdAsync(Guid id) =>
        await _db.CommissionSchemes
            .Include(s => s.Rules)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddSchemeAsync(CommissionScheme scheme) =>
        await _db.CommissionSchemes.AddAsync(scheme);

    public Task UpdateSchemeAsync(CommissionScheme scheme)
    {
        _db.CommissionSchemes.Update(scheme);
        return Task.CompletedTask;
    }

    public async Task DeleteSchemeAsync(Guid id)
    {
        var scheme = await _db.CommissionSchemes.FindAsync(id);
        if (scheme is not null)
            _db.CommissionSchemes.Remove(scheme);
    }

    public async Task<List<CommissionRule>> GetRulesBySchemeIdAsync(Guid schemeId) =>
        await _db.CommissionRules
            .Where(r => r.CommissionSchemeId == schemeId)
            .OrderBy(r => r.Priority)
            .ToListAsync();

    public async Task AddRuleAsync(CommissionRule rule) =>
        await _db.CommissionRules.AddAsync(rule);

    public Task UpdateRuleAsync(CommissionRule rule)
    {
        _db.CommissionRules.Update(rule);
        return Task.CompletedTask;
    }

    public async Task DeleteRuleAsync(Guid ruleId)
    {
        var rule = await _db.CommissionRules.FindAsync(ruleId);
        if (rule is not null)
            _db.CommissionRules.Remove(rule);
    }

    public async Task<List<CommissionAssignment>> GetAssignmentsBySchemeIdAsync(Guid schemeId) =>
        await _db.CommissionAssignments
            .Include(a => a.Employee)
            .Where(a => a.CommissionSchemeId == schemeId)
            .ToListAsync();

    public async Task<List<CommissionAssignment>> GetAssignmentsByEmployeeIdAsync(Guid employeeId) =>
        await _db.CommissionAssignments
            .Include(a => a.CommissionScheme)
            .Where(a => a.EmployeeId == employeeId)
            .ToListAsync();

    public async Task AddAssignmentAsync(CommissionAssignment assignment) =>
        await _db.CommissionAssignments.AddAsync(assignment);

    public Task UpdateAssignmentAsync(CommissionAssignment assignment)
    {
        _db.CommissionAssignments.Update(assignment);
        return Task.CompletedTask;
    }

    public async Task<List<CommissionRecord>> GetRecordsByPeriodAsync(Guid periodId, Guid companyId) =>
        await _db.CommissionRecords
            .Include(r => r.Employee)
            .Where(r => r.PayrollPeriodId == periodId && r.CompanyId == companyId)
            .ToListAsync();

    public async Task<List<CommissionRecord>> GetRecordsByEmployeeAsync(Guid employeeId) =>
        await _db.CommissionRecords
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<List<CommissionRecord>> GetRecordsBySaleIdAsync(Guid saleId) =>
        await _db.CommissionRecords
            .Where(r => r.SaleId == saleId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<CommissionRecord?> GetRecordByIdAsync(Guid id) =>
        await _db.CommissionRecords.FindAsync(id);

    public async Task AddRecordAsync(CommissionRecord record) =>
        await _db.CommissionRecords.AddAsync(record);

    public async Task AddRecordsAsync(List<CommissionRecord> records) =>
        await _db.CommissionRecords.AddRangeAsync(records);

    public Task UpdateRecordAsync(CommissionRecord record)
    {
        _db.CommissionRecords.Update(record);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
