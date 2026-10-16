using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IStockTransferRepository
{
    Task<StockTransfer?> GetByIdAsync(Guid id);

    /// <summary>
    /// <paramref name="branchId"/> como origen O destino (una transferencia es
    /// visible para ambas sucursales); Guid.Empty = todas las sucursales.
    /// </summary>
    Task<(List<StockTransfer> Items, int Total)> GetFilteredAsync(
        string? search, string? status,
        Guid branchId, int page, int pageSize);
    Task<string> GenerateTransferNumberAsync(Guid companyId);
    Task AddAsync(StockTransfer transfer);
    Task SaveChangesAsync();
}
