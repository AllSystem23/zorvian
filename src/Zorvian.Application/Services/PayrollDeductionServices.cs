using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class EmployeeLoanService
{
    private readonly IEmployeeLoanRepository _repo;
    private readonly ITenantContext _tenant;

    public EmployeeLoanService(IEmployeeLoanRepository repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    public async Task<EmployeeLoan> CreateLoanAsync(EmployeeLoan loan)
    {
        loan.TenantId = _tenant.TenantId;
        loan.Balance = loan.TotalAmount;
        await _repo.AddAsync(loan);
        await _repo.SaveChangesAsync();
        return loan;
    }

    public async Task<List<EmployeeLoan>> GetEmployeeLoansAsync(Guid employeeId) =>
        await _repo.GetActiveLoansAsync(employeeId, _tenant.TenantId);
}

public sealed class SalaryAdvanceService
{
    private readonly ISalaryAdvanceRepository _repo;
    private readonly ITenantContext _tenant;

    public SalaryAdvanceService(ISalaryAdvanceRepository repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    public async Task<SalaryAdvance> RequestAdvanceAsync(SalaryAdvance advance)
    {
        advance.TenantId = _tenant.TenantId;
        advance.Status = "pending";
        await _repo.AddAsync(advance);
        await _repo.SaveChangesAsync();
        return advance;
    }

    public async Task ApproveAdvanceAsync(Guid advanceId, Guid approverId)
    {
        var advance = await _repo.GetByIdAsync(advanceId, _tenant.TenantId);
        if (advance == null) throw new KeyNotFoundException("Advance not found");
        
        advance.Status = "approved";
        advance.ApprovedAt = DateTime.UtcNow;
        advance.ApprovedByEmployeeId = approverId;
        await _repo.UpdateAsync(advance);
        await _repo.SaveChangesAsync();
    }
}

public sealed class WageGarnishmentService
{
    private readonly IWageGarnishmentRepository _repo;
    private readonly ITenantContext _tenant;

    public WageGarnishmentService(IWageGarnishmentRepository repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    public async Task CreateGarnishmentAsync(WageGarnishment garnishment)
    {
        garnishment.TenantId = _tenant.TenantId;
        await _repo.AddAsync(garnishment);
        await _repo.SaveChangesAsync();
    }
}
