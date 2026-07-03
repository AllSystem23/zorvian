using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IFiscalYearRepository
{
    Task<List<FiscalYear>> GetAllAsync(Guid companyId);
    Task<FiscalYear?> GetByIdAsync(Guid id);
    Task<FiscalYear?> GetByYearAsync(int year, Guid companyId);
    Task AddAsync(FiscalYear entity);
    Task UpdateAsync(FiscalYear entity);
    Task DeleteAsync(FiscalYear entity);
    Task<int> SaveChangesAsync();
}
