using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Jobs;

public sealed class VacationAutomatedJob
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICountryTaxConfigRepository _taxConfigRepo;
    private readonly IVacationRepository _vacationRepo;
    private readonly IAuthRepository _authRepo;
    private readonly ITenantContextWriter _tenantWriter;

    public VacationAutomatedJob(
        IEmployeeRepository employeeRepo,
        ICountryTaxConfigRepository taxConfigRepo,
        IVacationRepository vacationRepo,
        IAuthRepository authRepo,
        ITenantContextWriter tenantWriter)
    {
        _employeeRepo = employeeRepo;
        _taxConfigRepo = taxConfigRepo;
        _vacationRepo = vacationRepo;
        _authRepo = authRepo;
        _tenantWriter = tenantWriter;
    }

    public async Task RunAsync()
    {
        var companies = await _authRepo.GetAllCompaniesAsync();
        var year = DateTime.UtcNow.Year;

        foreach (var company in companies.Where(c => !c.IsDeleted))
        {
            _tenantWriter.SetTenantId(company.TenantId);
            
            var employees = await _employeeRepo.GetFilteredAsync(null, "active", null, 1, 1000);
            
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
}
