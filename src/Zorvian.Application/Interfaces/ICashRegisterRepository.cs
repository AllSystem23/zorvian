using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICashRegisterRepository
{
    Task<CashRegister?> GetByIdAsync(Guid id);
    Task<CashRegister?> GetOpenByBranchAsync(Guid branchId);
    Task<List<CashRegister>> GetFilteredAsync(Guid? branchId, string? status, DateTime? fromDate, DateTime? toDate, Guid companyId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? branchId, string? status, DateTime? fromDate, DateTime? toDate, Guid companyId);
    Task<decimal> GetTodayIncomeAsync(Guid branchId);
    Task<decimal> GetTodayExpenseAsync(Guid branchId);
    Task<int> GetOpenRegistersCountAsync(Guid branchId);
    Task AddAsync(CashRegister cashRegister);
    Task UpdateAsync(CashRegister cashRegister);
    Task SaveChangesAsync();
}
