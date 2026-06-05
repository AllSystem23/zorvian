using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWithholdingRepository
{
    Task<Withholding?> GetByIdAsync(Guid id);
    Task<List<Withholding>> GetByPurchaseIdAsync(Guid purchaseId);
    Task AddAsync(Withholding withholding);
    Task SaveChangesAsync();
}
