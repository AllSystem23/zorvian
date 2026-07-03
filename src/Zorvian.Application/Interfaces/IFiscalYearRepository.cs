using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IFiscalYearRepository
{
    Task<List<FiscalYear>> GetAllAsync(Guid companyId);
    Task<FiscalYear?> GetByIdAsync(Guid id);
    Task<FiscalYear?> GetCurrentOpenAsync(Guid companyId);
    Task<FiscalYear?> GetByYearAsync(int year, Guid companyId);
    Task<FiscalYear?> GetByPeriodIdAsync(Guid periodId);
    Task AddAsync(FiscalYear fiscalYear);
    Task UpdateAsync(FiscalYear fiscalYear);
    Task DeleteAsync(FiscalYear fiscalYear);
    Task SaveChangesAsync();
}
