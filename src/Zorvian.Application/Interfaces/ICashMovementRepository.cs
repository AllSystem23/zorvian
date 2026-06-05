using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICashMovementRepository
{
    Task<CashMovement?> GetByIdAsync(Guid id);
    Task<List<CashMovement>> GetByCashRegisterIdAsync(Guid cashRegisterId);
    Task AddAsync(CashMovement movement);
    Task UpdateAsync(CashMovement movement);
    Task SaveChangesAsync();
}
