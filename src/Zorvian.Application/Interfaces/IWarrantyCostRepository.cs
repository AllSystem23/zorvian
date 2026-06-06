using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyCostRepository
{
    Task<List<WarrantyCost>> GetByWarrantyIdAsync(Guid warrantyId);
    Task<List<WarrantyCost>> GetByClaimIdAsync(Guid claimId);
    Task<WarrantyCost?> GetByIdAsync(Guid id);
    Task AddAsync(WarrantyCost cost);
    Task UpdateAsync(WarrantyCost cost);
    Task DeleteAsync(WarrantyCost cost);
    Task SaveChangesAsync();

    Task<decimal> GetTotalCostByWarrantyAsync(Guid warrantyId);
    Task<List<(string Category, decimal Total)>> GetCostBreakdownByWarrantyAsync(Guid warrantyId);
    Task<decimal> GetTotalCostByPeriodAsync(Guid companyId, DateTime from, DateTime to);
    Task<List<(string Category, decimal Total)>> GetCostBreakdownByPeriodAsync(Guid companyId, DateTime from, DateTime to);
    Task<List<(int Year, int Month, decimal Total)>> GetMonthlyCostTrendAsync(Guid companyId, int months);
}
