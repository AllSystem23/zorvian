using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class EmployeeLoanService
{
    private readonly IEmployeeLoanRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IEntityHistoryRepository _history;

    public EmployeeLoanService(IEmployeeLoanRepository repo, ITenantContext tenant, IEntityHistoryRepository history)
    {
        _repo = repo;
        _tenant = tenant;
        _history = history;
    }

    public async Task<EmployeeLoan> CreateLoanAsync(EmployeeLoan loan)
    {
        loan.TenantId = _tenant.TenantId;
        loan.Balance = loan.TotalAmount;
        await _repo.AddAsync(loan);
        await _repo.SaveChangesAsync();

        var entries = EntityHistoryHelper.CreateEntry("EmployeeLoan", loan.Id, "Create", "Loan",
            $"{loan.PrincipalAmount} @ {loan.InterestRate}%");
        foreach (var e in entries) e.TenantId = _tenant.TenantId;
        await _history.AddRangeAsync(entries);
        await _history.SaveChangesAsync();

        return loan;
    }

    public async Task<List<EmployeeLoan>> GetEmployeeLoansAsync(Guid employeeId) =>
        await _repo.GetActiveLoansAsync(employeeId, _tenant.TenantId);
}

public sealed class SalaryAdvanceService
{
    private readonly ISalaryAdvanceRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IEntityHistoryRepository _history;

    public SalaryAdvanceService(ISalaryAdvanceRepository repo, ITenantContext tenant, IEntityHistoryRepository history)
    {
        _repo = repo;
        _tenant = tenant;
        _history = history;
    }

    public async Task<SalaryAdvance> RequestAdvanceAsync(SalaryAdvance advance)
    {
        advance.TenantId = _tenant.TenantId;
        advance.Status = "pending";
        await _repo.AddAsync(advance);
        await _repo.SaveChangesAsync();

        var entries = EntityHistoryHelper.CreateEntry("SalaryAdvance", advance.Id, "Create", "Advance",
            $"{advance.RequestedAmount} requested");
        foreach (var e in entries) e.TenantId = _tenant.TenantId;
        await _history.AddRangeAsync(entries);
        await _history.SaveChangesAsync();

        return advance;
    }

    public async Task ApproveAdvanceAsync(Guid advanceId, Guid approverId)
    {
        var advance = await _repo.GetByIdAsync(advanceId, _tenant.TenantId);
        if (advance == null) throw new KeyNotFoundException("Advance not found");

        var entries = EntityHistoryHelper.CreateEntry("SalaryAdvance", advanceId, "Update", "Status",
            "approved", advance.Status);
        foreach (var e in entries) e.TenantId = _tenant.TenantId;

        advance.Status = "approved";
        advance.ApprovedAt = DateTime.UtcNow;
        advance.ApprovedByEmployeeId = approverId;
        await _repo.UpdateAsync(advance);
        await _repo.SaveChangesAsync();

        await _history.AddRangeAsync(entries);
        await _history.SaveChangesAsync();
    }
}

public sealed class WageGarnishmentService
{
    private readonly IWageGarnishmentRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IEntityHistoryRepository _history;

    public WageGarnishmentService(IWageGarnishmentRepository repo, ITenantContext tenant, IEntityHistoryRepository history)
    {
        _repo = repo;
        _tenant = tenant;
        _history = history;
    }

    public async Task CreateGarnishmentAsync(WageGarnishment garnishment)
    {
        garnishment.TenantId = _tenant.TenantId;
        await _repo.AddAsync(garnishment);
        await _repo.SaveChangesAsync();

        var entries = EntityHistoryHelper.CreateEntry("WageGarnishment", garnishment.Id, "Create", "Garnishment",
            $"{garnishment.Value} from employee {garnishment.EmployeeId}");
        foreach (var e in entries) e.TenantId = _tenant.TenantId;
        await _history.AddRangeAsync(entries);
        await _history.SaveChangesAsync();
    }
}
