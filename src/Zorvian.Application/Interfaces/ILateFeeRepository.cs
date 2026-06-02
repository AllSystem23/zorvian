using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ILateFeeRepository
{
    Task<LateFee?> GetByIdAsync(Guid id);
    Task<List<LateFee>> GetByCreditIdAsync(Guid creditId);
    Task<List<LateFee>> GetByInstallmentIdAsync(Guid installmentId);
    Task<List<LateFee>> GetPendingByCreditIdAsync(Guid creditId);
    Task<LateFee?> GetByInstallmentAndDateAsync(Guid installmentId, DateOnly calculatedAt);
    Task AddAsync(LateFee lateFee);
    Task UpdateAsync(LateFee lateFee);
    Task AddRangeAsync(IEnumerable<LateFee> lateFees);
    Task SaveChangesAsync();
}
