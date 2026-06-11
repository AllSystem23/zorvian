using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Jobs;

public sealed class VacationAutomatedJob
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICountryTaxConfigRepository _taxConfigRepo;
    private readonly IVacationRepository _vacationRepo;

    public VacationAutomatedJob(
        IEmployeeRepository employeeRepo,
        ICountryTaxConfigRepository taxConfigRepo,
        IVacationRepository vacationRepo)
    {
        _employeeRepo = employeeRepo;
        _taxConfigRepo = taxConfigRepo;
        _vacationRepo = vacationRepo;
    }

    public async Task RunAsync()
    {
        var employees = await _employeeRepo.GetFilteredAsync(null, "active", null, 1, 1000);
        var year = DateTime.UtcNow.Year;
        
        foreach (var employee in employees)
        {
            var config = await _taxConfigRepo.GetByCountryCodeAsync(employee.CountryCode);
            if (config == null) continue;

            var balance = await _vacationRepo.GetLeaveBalanceAsync(employee.Id, year) 
                ?? new LeaveBalances { EmployeeId = employee.Id, Year = year };

            // Acumulación mensual: (días anuales / 12)
            balance.VacationDaysAccrued += (decimal)config.VacationDaysPerYear / 12;
            
            if (balance.Id == Guid.Empty)
                await _vacationRepo.AddLeaveBalanceAsync(balance);
                
            await _vacationRepo.SaveChangesAsync();
        }
    }
}
