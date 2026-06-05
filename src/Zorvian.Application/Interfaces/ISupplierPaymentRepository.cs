using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ISupplierPaymentRepository
{
    Task<SupplierPayment?> GetByIdAsync(Guid id);
    Task<List<SupplierPayment>> GetByPurchaseIdAsync(Guid purchaseId);
    Task AddAsync(SupplierPayment payment);
    Task SaveChangesAsync();
}
