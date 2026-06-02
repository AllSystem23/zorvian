using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface ICashMovementRepository
{
    Task<List<CashMovement>> GetByCashRegisterIdAsync(Guid cashRegisterId);
    Task AddAsync(CashMovement movement);
    Task SaveChangesAsync();
}
