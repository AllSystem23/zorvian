using AutoMapper;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public class InventoryMovementService : IInventoryMovementService
{
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly IProductRepository _productRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly IGoalIntegrationService _goalIntegration;

    public InventoryMovementService(
        IInventoryMovementRepository movementRepo,
        IProductRepository productRepo,
        IAutoAccountingService autoAccounting,
        ITenantContext tenant,
        IMapper mapper,
        IGoalIntegrationService goalIntegration)
    {
        _movementRepo = movementRepo;
        _productRepo = productRepo;
        _autoAccounting = autoAccounting;
        _tenant = tenant;
        _mapper = mapper;
        _goalIntegration = goalIntegration;
    }

    private Guid RequireCompanyId()
    {
        if (_tenant.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Seleccione una empresa antes de registrar movimientos de inventario.");

        return _tenant.TenantId.Value;
    }

    private Guid? ResolveBranchId(Guid requestBranchId)
    {
        if (requestBranchId != Guid.Empty)
            return requestBranchId;

        return _tenant.IsSuperAdmin ? null : _tenant.EffectiveCompanyId;
    }

    public async Task<InventoryMovementResponse> CreateAsync(CreateInventoryMovementRequest request)
    {
        var companyId = RequireCompanyId();
        var branchId = ResolveBranchId(request.BranchId);

        var product = await _productRepo.GetByIdAsync(request.ProductId)
            ?? throw new InvalidOperationException("Product not found");

        var stockBefore = product.Stock;
        var stockAfter = request.MovementType switch
        {
            "entry" => product.Stock + request.Quantity,
            "exit" or "adjustment_negative" => Math.Max(0, product.Stock - request.Quantity),
            "adjustment_positive" => product.Stock + request.Quantity,
            _ => product.Stock,
        };

        product.Stock = stockAfter;

        var movement = _mapper.Map<InventoryMovement>(request);
        movement.StockBefore = stockBefore;
        movement.StockAfter = stockAfter;
        movement.BranchId = branchId ?? Guid.Empty;
        movement.TenantId = _tenant.TenantId.ToString();

        await _movementRepo.AddAsync(movement);
        await _movementRepo.SaveChangesAsync();

        await _autoAccounting.GenerateInventoryEntryAsync(
            movement.Id, movement.ProductId, movement.MovementType, movement.Quantity, request.UnitCost);

        // Disparar evento para meta de entrega si es tipo salida
        if (request.MovementType == "exit")
        {
            await _goalIntegration.HandleDeliveryAsync(movement.PerformedByEmployeeId ?? Guid.Empty, movement.Id, movement.Quantity);
        }

        return _mapper.Map<InventoryMovementResponse>(movement);
    }

    public async Task<InventoryMovementResponse?> GetByIdAsync(Guid id)
    {
        var movement = await _movementRepo.GetByIdAsync(id);
        return movement is null ? null : _mapper.Map<InventoryMovementResponse>(movement);
    }

    public async Task<PagedResult<InventoryMovementResponse>> GetFilteredAsync(InventoryMovementFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var branchId = _tenant.IsSuperAdmin ? null : _tenant.EffectiveCompanyId;

        var items = await _movementRepo.GetFilteredAsync(filter.ProductId, filter.MovementType, filter.FromDate, filter.ToDate, filter.Search, branchId, page, pageSize);
        var total = await _movementRepo.GetFilteredCountAsync(filter.ProductId, filter.MovementType, filter.FromDate, filter.ToDate, filter.Search, branchId);

        return new PagedResult<InventoryMovementResponse>(
            _mapper.Map<List<InventoryMovementResponse>>(items),
            total, page, pageSize
        );
    }
}
