using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class VacationRecommendationService : IVacationRecommendationService
{
    private readonly IVacationRepository _vacationRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ITenantContext _tenant;

    public VacationRecommendationService(IVacationRepository vacationRepo, IEmployeeRepository employeeRepo, ITenantContext tenant)
    {
        _vacationRepo = vacationRepo;
        _employeeRepo = employeeRepo;
        _tenant = tenant;
    }

    public async Task<List<DateOnly>> RecommendDatesAsync(Guid employeeId, int daysRequested, int month, int year)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId);
        if (employee == null) return new List<DateOnly>();

        var departmentId = employee.DepartmentId ?? Guid.Empty;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var recommended = new List<DateOnly>();

        // Find consecutive days with less than 30% overlap in the department
        for (int day = 1; day <= daysInMonth - daysRequested + 1; day++)
        {
            var start = new DateOnly(year, month, day);
            var end = start.AddDays(daysRequested - 1);

            var overlappingCount = await _vacationRepo.GetOverlappingCountAsync(departmentId, start, end, employeeId);
            
            // Assuming simplified rule: no more than 30% overlap allowed.
            // A more robust implementation would fetch total employees in department.
            if (overlappingCount < 2) // Simplified threshold
            {
                recommended.Add(start);
                // Return just the first valid start date found for now
                break;
            }
        }

        return recommended;
    }
}
