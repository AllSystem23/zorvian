using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICashRegisterArqueoRepository
{
    Task<CashRegisterArqueo?> GetByRegisterIdAsync(Guid cashRegisterId);
    Task AddAsync(CashRegisterArqueo arqueo);
    Task SaveChangesAsync();
}
