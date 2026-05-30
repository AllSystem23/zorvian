using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class PayrollRepository : IPayrollRepository
{
    private readonly NexoraDbContext _db;

    public PayrollRepository(NexoraDbContext db) => _db = db;

    // Deduction Types
    public async Task<List<DeductionType>> GetDeductionTypesAsync() =>
        await _db.DeductionTypes.OrderBy(d => d.Priority).ThenBy(d => d.Code).ToListAsync();

    public async Task<DeductionType?> GetDeductionTypeByIdAsync(Guid id) =>
        await _db.DeductionTypes.FindAsync(id);

    public async Task AddDeductionTypeAsync(DeductionType type) =>
        await _db.DeductionTypes.AddAsync(type);

    public Task UpdateDeductionTypeAsync(DeductionType type) =>
        Task.FromResult(_db.DeductionTypes.Update(type));

    public async Task DeleteDeductionTypeAsync(Guid id)
    {
        var entity = await _db.DeductionTypes.FindAsync(id);
        if (entity != null) _db.DeductionTypes.Remove(entity);
    }

    // Employee Salaries
    public async Task<List<EmployeeSalary>> GetSalariesAsync(Guid? employeeId)
    {
        var query = _db.EmployeeSalaries.Include(s => s.Employee).AsQueryable();
        if (employeeId.HasValue) query = query.Where(s => s.EmployeeId == employeeId.Value);
        return await query.OrderByDescending(s => s.EffectiveDate).ToListAsync();
    }

    public async Task<EmployeeSalary?> GetActiveSalaryAsync(Guid employeeId) =>
        await _db.EmployeeSalaries
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.IsActive);

    public async Task AddSalaryAsync(EmployeeSalary salary) =>
        await _db.EmployeeSalaries.AddAsync(salary);

    public Task UpdateSalaryAsync(EmployeeSalary salary) =>
        Task.FromResult(_db.EmployeeSalaries.Update(salary));

    // Payroll Periods
    public async Task<List<PayrollPeriod>> GetPeriodsAsync(int? year)
    {
        var query = _db.PayrollPeriods.AsQueryable();
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);
        return await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ThenBy(p => p.PeriodNumber).ToListAsync();
    }

    public async Task<PayrollPeriod?> GetPeriodByIdAsync(Guid id) =>
        await _db.PayrollPeriods.FindAsync(id);

    public async Task<PayrollPeriod?> GetOpenPeriodAsync() =>
        await _db.PayrollPeriods.FirstOrDefaultAsync(p => p.Status == "open");

    public async Task AddPeriodAsync(PayrollPeriod period) =>
        await _db.PayrollPeriods.AddAsync(period);

    public Task UpdatePeriodAsync(PayrollPeriod period) =>
        Task.FromResult(_db.PayrollPeriods.Update(period));

    // Payroll Runs
    public async Task<List<PayrollRun>> GetRunsAsync(Guid? periodId)
    {
        var query = _db.PayrollRuns.Include(r => r.PayrollPeriod).Include(r => r.Details).ThenInclude(d => d.Employee).AsQueryable();
        if (periodId.HasValue) query = query.Where(r => r.PayrollPeriodId == periodId.Value);
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<PayrollRun?> GetRunByIdAsync(Guid id) =>
        await _db.PayrollRuns.Include(r => r.PayrollPeriod).Include(r => r.Details).ThenInclude(d => d.Employee).FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddRunAsync(PayrollRun run) =>
        await _db.PayrollRuns.AddAsync(run);

    public Task UpdateRunAsync(PayrollRun run) =>
        Task.FromResult(_db.PayrollRuns.Update(run));

    // Payroll Details
    public async Task AddDetailsAsync(List<PayrollDetail> details) =>
        await _db.PayrollDetails.AddRangeAsync(details);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
