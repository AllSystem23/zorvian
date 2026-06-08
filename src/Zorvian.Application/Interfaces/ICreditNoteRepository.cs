using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICreditNoteRepository
{
    Task<List<CreditNote>> GetAllAsync(Guid companyId);
    Task<CreditNote?> GetByIdAsync(Guid id);
    Task<List<CreditNote>> GetBySaleIdAsync(Guid saleId);
    Task<string> GenerateCreditNoteNumberAsync(Guid companyId);
    Task AddAsync(CreditNote creditNote);
    Task SaveChangesAsync();
}
