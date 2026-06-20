using Zorvian.Application.DTOs.Credit;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICreditRepository
{
    Task<Credit?> GetByIdAsync(Guid id);
    Task<List<Credit>> GetFilteredAsync(Guid? clientId, string? status, string? search, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? clientId, string? status, string? search, Guid branchId);
    Task<string> GenerateCreditNumberAsync(Guid companyId);
    Task<int> GetActiveCreditsCountAsync(Guid branchId);
    Task<int> GetOverdueCreditsCountAsync(Guid branchId);
    Task<decimal> GetMonthlyRecoveryAsync(Guid branchId);
    Task<decimal> GetTotalPortfolioAsync(Guid branchId);
    Task<OverdueDashboardScalars> GetOverdueDashboardScalarsRawAsync(Guid branchId);
    Task<decimal> GetArAgingTotalPortfolioRawAsync(Guid? companyId, bool isSuperAdmin, string currency);
    Task<List<CreditAgingClientItem>> GetArAgingClientsRawAsync(Guid? companyId, bool isSuperAdmin, string currency);
    Task<List<CreditInstallment>> GetOverdueInstallmentsAsync(Guid branchId);
    Task<List<CreditInstallment>> GetCriticalOverdueInstallmentsAsync(Guid branchId, int limit);
    Task<List<CreditInstallment>> GetInstallmentsByCreditIdAsync(Guid creditId);
    Task AddAsync(Credit credit);
    Task UpdateAsync(Credit credit);
    Task SaveChangesAsync();
}
