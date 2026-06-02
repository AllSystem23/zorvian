using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICreditPaymentRepository
{
    Task<List<CreditPayment>> GetByCreditIdAsync(Guid creditId, int page, int pageSize);
    Task<int> GetCountByCreditIdAsync(Guid creditId);
    Task AddAsync(CreditPayment payment);
    Task SaveChangesAsync();
}
