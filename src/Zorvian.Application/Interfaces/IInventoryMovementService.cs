using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Inventory;

namespace Zorvian.Application.Interfaces;

public interface IInventoryMovementService
{
    Task<InventoryMovementResponse> CreateAsync(CreateInventoryMovementRequest request);
    Task<PagedResult<InventoryMovementResponse>> GetFilteredAsync(InventoryMovementFilterRequest filter);
}
