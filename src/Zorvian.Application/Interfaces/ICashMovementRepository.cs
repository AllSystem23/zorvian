using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICashMovementRepository
{
    Task<List<CashMovement>> GetByCashRegisterIdAsync(Guid cashRegisterId);
    Task AddAsync(CashMovement movement);
    Task SaveChangesAsync();
}
