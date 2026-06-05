using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

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
    Task<EmployeeSalary?> GetSalaryByIdAsync(Guid id);
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
    Task DeleteRunAsync(Guid id);

    // Payroll Details
    Task AddDetailsAsync(List<PayrollDetail> details);
    Task<PayrollDetail?> GetDetailByIdAsync(Guid id);
    Task<PayrollDetail?> GetDetailByReferenceAsync(string reference);
    Task UpdateDetailAsync(PayrollDetail detail);

    Task SaveChangesAsync();
}
