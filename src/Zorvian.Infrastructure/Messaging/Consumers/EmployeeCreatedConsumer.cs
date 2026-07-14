using MassTransit;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Messages;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles EmployeeCreatedEvent: initializes leave balances and creates default payroll salary record.
/// Phase 2 implementation — creates LeaveBalances for the current year and an active EmployeeSalary.
/// </summary>
public sealed class EmployeeCreatedConsumer : IConsumer<EmployeeCreatedEvent>
{
    private readonly ILogger<EmployeeCreatedConsumer> _logger;
    private readonly IVacationRepository _vacationRepo;
    private readonly IPayrollRepository _payrollRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ITenantContextWriter _tenantWriter;

    public EmployeeCreatedConsumer(
        ILogger<EmployeeCreatedConsumer> logger,
        IVacationRepository vacationRepo,
        IPayrollRepository payrollRepo,
        ICompanyRepository companyRepo,
        ITenantContextWriter tenantWriter)
    {
        _logger = logger;
        _vacationRepo = vacationRepo;
        _payrollRepo = payrollRepo;
        _companyRepo = companyRepo;
        _tenantWriter = tenantWriter;
    }

    public async Task Consume(ConsumeContext<EmployeeCreatedEvent> context)
    {
        var employee = context.Message;
        _logger.LogInformation(
            "EmployeeCreated event: EmployeeId={EmployeeId}, Name={Name} {LastName}, Position={Position}, Salary={Salary}, Country={Country}",
            employee.EmployeeId, employee.FirstName, employee.LastName, employee.Position, employee.Salary, employee.CountryCode);

        // ── 0. Set multi-tenant context ──
        // The consumer runs outside HTTP scope, so we must explicitly set the tenant
        // context to ensure EF Core query filters and save interceptor apply correctly.
        var company = await _companyRepo.GetByIdAsync(employee.CompanyId);
        if (company is null)
        {
            _logger.LogError("Company {CompanyId} not found for employee {EmployeeId} — aborting consumer", employee.CompanyId, employee.EmployeeId);
            return;
        }
        _tenantWriter.SetTenantId(company.TenantId);
        _logger.LogDebug("Tenant context set to TenantId={TenantId} for CompanyId={CompanyId}", company.TenantId, employee.CompanyId);

        var currentYear = employee.HireDate.Year;

        // ── 1. Initialize LeaveBalances for the current year ──
        var existingBalance = await _vacationRepo.GetLeaveBalanceAsync(employee.EmployeeId, currentYear);
        if (existingBalance is null)
        {
            var balance = new LeaveBalances
            {
                EmployeeId = employee.EmployeeId,
                Year = currentYear,
                // Start with zero accruals — the monthly VacationAutomatedJob will
                // accrue (VacationDaysPerYear / 12) each month going forward.
                VacationDaysAccrued = 0m,
                VacationDaysTaken = 0m,
                VacationDaysPending = 0m,
                SickDaysAccrued = 0m,
                SickDaysTaken = 0m,
                PersonalDaysAccrued = 0m,
                PersonalDaysTaken = 0m,
            };

            await _vacationRepo.AddLeaveBalanceAsync(balance);
            _logger.LogDebug("LeaveBalances created for EmployeeId={EmployeeId}, Year={Year}", employee.EmployeeId, currentYear);
        }

        // ── 2. Create active EmployeeSalary record for payroll ──
        if (employee.Salary > 0)
        {
            var activeSalary = await _payrollRepo.GetActiveSalaryAsync(employee.EmployeeId);
            if (activeSalary is null)
            {
                var salary = new EmployeeSalary
                {
                    EmployeeId = employee.EmployeeId,
                    BaseSalary = employee.Salary,
                    SalaryType = "monthly",
                    EffectiveDate = DateOnly.FromDateTime(employee.HireDate),
                    IsActive = true,
                    Notes = $"Registro inicial creado desde EmployeeCreatedConsumer",
                };

                await _payrollRepo.AddSalaryAsync(salary);
                _logger.LogDebug("EmployeeSalary created for EmployeeId={EmployeeId}, BaseSalary={Salary}", employee.EmployeeId, employee.Salary);
            }
        }
        else
        {
            _logger.LogWarning("EmployeeId={EmployeeId} has zero salary — skipping EmployeeSalary creation", employee.EmployeeId);
        }

        // ── 3. Persist all changes ──
        // Both repositories share the same ZorvianDbContext (scoped), so a single SaveChanges is enough.
        await _vacationRepo.SaveChangesAsync();

        _logger.LogInformation(
            "EmployeeCreatedConsumer completed for EmployeeId={EmployeeId}: LeaveBalances={LeaveBalanceCreated}, EmployeeSalary={SalaryCreated}",
            employee.EmployeeId, existingBalance is null, employee.Salary > 0);
    }
}
