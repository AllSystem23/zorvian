using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class TerminationService
{
    private readonly ITerminationRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IBenefitProvisionRepository _benefitRepo;

    public TerminationService(ITerminationRepository repo, IEmployeeRepository employeeRepo, IBenefitProvisionRepository benefitRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _benefitRepo = benefitRepo;
    }

    public async Task<TerminationRecord?> CalculateAsync(Guid employeeId, TerminationReason reason, DateOnly terminationDate)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId) 
            ?? throw new InvalidOperationException("Employee not found");
        
        // Fetch accrued benefits
        var provisions = await _benefitRepo.GetByEmployeeAsync(employeeId);
        var vacationPay = provisions.Where(p => p.BenefitType == "vacation").Sum(p => p.Amount);
        var aguinaldoPay = provisions.Where(p => p.BenefitType == "aguinaldo").Sum(p => p.Amount);
        
        // Simple severance: Based on years of service (max 5 months)
        var yearsOfService = (terminationDate.DayNumber - employee.HireDate.DayNumber) / 365;
        var severanceDays = Math.Min(yearsOfService * 30, 150); // Nicaragua cap: 5 months max
        var dailyWage = (employee.Salary ?? 0) / 30;
        var severancePay = dailyWage * severanceDays;

        var record = new TerminationRecord
        {
            EmployeeId = employeeId,
            TerminationDate = terminationDate,
            Reason = reason,
            GrossSettlement = severancePay + vacationPay + aguinaldoPay,
            NetSettlement = severancePay + vacationPay + aguinaldoPay, // Taxes may apply
            SeveranceDays = severanceDays,
            AccruedVacationPay = vacationPay,
            AccruedAguinaldoPay = aguinaldoPay,
            Status = "draft"
        };

        await _repo.AddAsync(record);
        await _repo.SaveChangesAsync();
        return record;
    }
}
