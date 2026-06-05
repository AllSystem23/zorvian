using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ISupplierCreditNoteRepository
{
    Task<SupplierCreditNote?> GetByIdAsync(Guid id);
    Task<List<SupplierCreditNote>> GetBySupplierIdAsync(Guid supplierId);
    Task<List<SupplierCreditNote>> GetByPurchaseIdAsync(Guid purchaseId);
    Task<string> GenerateCreditNoteNumberAsync(Guid companyId);
    Task AddAsync(SupplierCreditNote creditNote);
    Task SaveChangesAsync();
}
