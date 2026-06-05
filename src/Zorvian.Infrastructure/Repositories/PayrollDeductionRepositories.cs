using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class EmployeeLoanRepository : IEmployeeLoanRepository
{
    private readonly ZorvianDbContext _db;
    public EmployeeLoanRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<EmployeeLoan>> GetActiveLoansAsync(Guid employeeId, string tenantId) =>
        await _db.EmployeeLoans.Where(l => l.EmployeeId == employeeId && l.Status == "active").ToListAsync();

    public async Task<EmployeeLoan?> GetByIdAsync(Guid id, string tenantId) =>
        await _db.EmployeeLoans.FirstOrDefaultAsync(l => l.Id == id);

    public async Task AddAsync(EmployeeLoan loan) { await _db.EmployeeLoans.AddAsync(loan); await Task.CompletedTask; }

    public async Task UpdateAsync(EmployeeLoan loan) { _db.EmployeeLoans.Update(loan); await Task.CompletedTask; }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
    }

    public sealed class SalaryAdvanceRepository : ISalaryAdvanceRepository
    {
    private readonly ZorvianDbContext _db;
    public SalaryAdvanceRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<SalaryAdvance>> GetByEmployeeAsync(Guid employeeId, string tenantId) =>
        await _db.SalaryAdvances.Where(a => a.EmployeeId == employeeId).ToListAsync();

    public async Task<SalaryAdvance?> GetByIdAsync(Guid id, string tenantId) =>
        await _db.SalaryAdvances.FirstOrDefaultAsync(a => a.Id == id);

    public async Task AddAsync(SalaryAdvance advance) { await _db.SalaryAdvances.AddAsync(advance); await Task.CompletedTask; }

    public async Task UpdateAsync(SalaryAdvance advance) { _db.SalaryAdvances.Update(advance); await Task.CompletedTask; }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
    }

    public sealed class WageGarnishmentRepository : IWageGarnishmentRepository
    {
    private readonly ZorvianDbContext _db;
    public WageGarnishmentRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<WageGarnishment>> GetActiveGarnishmentsAsync(Guid employeeId, string tenantId) =>
        await _db.WageGarnishments.Where(g => g.EmployeeId == employeeId && g.Status == "active").ToListAsync();

    public async Task<WageGarnishment?> GetByIdAsync(Guid id, string tenantId) =>
        await _db.WageGarnishments.FirstOrDefaultAsync(g => g.Id == id);

    public async Task AddAsync(WageGarnishment garnishment) { await _db.WageGarnishments.AddAsync(garnishment); await Task.CompletedTask; }

    public async Task UpdateAsync(WageGarnishment garnishment) { _db.WageGarnishments.Update(garnishment); await Task.CompletedTask; }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
