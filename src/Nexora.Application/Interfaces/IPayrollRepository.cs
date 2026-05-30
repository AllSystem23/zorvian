using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IPayrollRepository
{
    // Deduction Types
    Task<List<DeductionType>> GetDeductionTypesAsync();
    Task<DeductionType?> GetDeductionTypeByIdAsync(Guid id);
    Task AddDeductionTypeAsync(DeductionType type);
    Task UpdateDeductionTypeAsync(DeductionType type);
    Task DeleteDeductionTypeAsync(Guid id);

    // Employee Salaries
    Task<List<EmployeeSalary>> GetSalariesAsync(Guid? employeeId);
    Task<EmployeeSalary?> GetActiveSalaryAsync(Guid employeeId);
    Task AddSalaryAsync(EmployeeSalary salary);
    Task UpdateSalaryAsync(EmployeeSalary salary);

    // Payroll Periods
    Task<List<PayrollPeriod>> GetPeriodsAsync(int? year);
    Task<PayrollPeriod?> GetPeriodByIdAsync(Guid id);
    Task<PayrollPeriod?> GetOpenPeriodAsync();
    Task AddPeriodAsync(PayrollPeriod period);
    Task UpdatePeriodAsync(PayrollPeriod period);

    // Payroll Runs
    Task<List<PayrollRun>> GetRunsAsync(Guid? periodId);
    Task<PayrollRun?> GetRunByIdAsync(Guid id);
    Task AddRunAsync(PayrollRun run);
    Task UpdateRunAsync(PayrollRun run);

    // Payroll Details
    Task AddDetailsAsync(List<PayrollDetail> details);

    Task SaveChangesAsync();
}
