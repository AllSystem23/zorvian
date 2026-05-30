using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IVacationRepository
{
    Task<VacationRequest?> GetByIdAsync(Guid id);
    Task<List<VacationRequest>> GetFilteredAsync(string? status, Guid? employeeId, int? year, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? status, Guid? employeeId, int? year);
    Task<decimal> GetVacationDaysSumAsync(Guid employeeId, string status);
    Task<int> GetOverlappingCountAsync(Guid departmentId, DateOnly start, DateOnly end, Guid excludeEmployeeId);
    Task AddAsync(VacationRequest request);
    Task SaveChangesAsync();
}
