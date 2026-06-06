using AutoMapper;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyPartUsageService
{
    private readonly IWarrantyPartUsageRepository _repo;
    private readonly IInventoryMovementService _inventoryService;
    private readonly IWarrantyCostService _costService;
    private readonly IMapper _mapper;

    public WarrantyPartUsageService(
        IWarrantyPartUsageRepository repo,
        IInventoryMovementService inventoryService,
        IWarrantyCostService costService,
        IMapper mapper)
    {
        _repo = repo;
        _inventoryService = inventoryService;
        _costService = costService;
        _mapper = mapper;
    }

    public async Task<WarrantyPartUsageResponse> RecordUsageAsync(CreateWarrantyPartUsageRequest request, Guid branchId, Guid warrantyId)
    {
        // Without IUnitOfWork, atomicity across services is hard.
        // Assuming repository's SaveChangesAsync is sufficient for now within this service context.
        
        // 1. Registrar salida de inventario
        var movement = await _inventoryService.CreateAsync(new CreateInventoryMovementRequest(
            request.ProductId,
            "exit",
            request.QuantityUsed,
            0,
            $"Uso en garantía (Claim {request.ClaimId})",
            request.Notes,
            branchId
        ));

        // 2. Registrar el uso
        var usage = _mapper.Map<WarrantyPartUsage>(request);
        usage.UnitCost = movement.UnitCost;
        
        await _repo.AddAsync(usage);
        await _repo.SaveChangesAsync();

        // 3. Registrar costo
        await _costService.CreateAsync(new CreateWarrantyCostRequest(
            warrantyId,
            request.ClaimId,
            "parts",
            request.Notes,
            request.QuantityUsed,
            usage.UnitCost,
            "company",
            null,
            null,
            null,
            true,
            request.Notes
        ));

        return _mapper.Map<WarrantyPartUsageResponse>(usage);
    }
}
