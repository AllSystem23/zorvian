using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IEmployeeLoanRepository
{
    Task<List<EmployeeLoan>> GetActiveLoansAsync(Guid employeeId, string tenantId);
    Task<EmployeeLoan?> GetByIdAsync(Guid id, string tenantId);
    Task AddAsync(EmployeeLoan loan);
    Task UpdateAsync(EmployeeLoan loan);
    Task SaveChangesAsync();
}

public interface ISalaryAdvanceRepository
{
    Task<List<SalaryAdvance>> GetByEmployeeAsync(Guid employeeId, string tenantId);
    Task<SalaryAdvance?> GetByIdAsync(Guid id, string tenantId);
    Task AddAsync(SalaryAdvance advance);
    Task UpdateAsync(SalaryAdvance advance);
    Task SaveChangesAsync();
}

public interface IWageGarnishmentRepository
{
    Task<List<WageGarnishment>> GetActiveGarnishmentsAsync(Guid employeeId, string tenantId);
    Task<WageGarnishment?> GetByIdAsync(Guid id, string tenantId);
    Task AddAsync(WageGarnishment garnishment);
    Task UpdateAsync(WageGarnishment garnishment);
    Task SaveChangesAsync();
}
