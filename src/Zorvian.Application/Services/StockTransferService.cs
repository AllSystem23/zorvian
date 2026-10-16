using Zorvian.Application.DTOs.StockTransfer;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

/// <summary>
/// Transferencia de stock entre DOS bodegas (misma sucursal o de sucursales
/// distintas) de la misma compañía.
/// Máquina de estados: draft → in_transit (despacho: baja + movimiento en la
/// bodega ORIGEN) → partially_received (recepción parcial, re-receivable) →
/// completed (todo recibido en la bodega DESTINO, con clon del producto si la
/// bodega destino aún no lo maneja); cancelled solo desde draft.
/// FromBranchId/ToBranchId se derivan de las bodegas al crear.
/// </summary>
public sealed class StockTransferService
{
    private readonly IStockTransferRepository _repo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IBranchValidator _branchValidator;
    private readonly IWebhookService _webhook;
    private readonly ITenantContext _tenant;

    public StockTransferService(
        IStockTransferRepository repo,
        IProductRepository productRepo,
        IInventoryMovementRepository movementRepo,
        IWarehouseRepository warehouseRepo,
        IBranchValidator branchValidator,
        IWebhookService webhook,
        ITenantContext tenant)
    {
        _repo = repo;
        _productRepo = productRepo;
        _movementRepo = movementRepo;
        _warehouseRepo = warehouseRepo;
        _branchValidator = branchValidator;
        _webhook = webhook;
        _tenant = tenant;
    }

    public async Task<StockTransferResponse> CreateAsync(CreateStockTransferRequest request)
    {
        if (request.Details is null || request.Details.Count == 0)
            throw new InvalidOperationException("La transferencia debe incluir al menos un producto.");
        if (request.Details.Any(d => d.Quantity <= 0))
            throw new InvalidOperationException("Todas las cantidades deben ser mayores a cero.");

        var fromWarehouse = await ResolveWarehouseAsync(
            request.FromWarehouseId, request.FromBranchId, "origen", allowContextFallback: true);
        var toWarehouse = await ResolveWarehouseAsync(
            request.ToWarehouseId, request.ToBranchId, "destino", allowContextFallback: false);

        if (fromWarehouse.Id == toWarehouse.Id)
            throw new InvalidOperationException("La bodega origen y destino deben ser distintas.");

        var companyId = RequireCompanyId();

        var details = new List<StockTransferDetail>(request.Details.Count);
        foreach (var line in request.Details)
        {
            var originProduct = await ResolveOriginProductAsync(line.ProductId, fromWarehouse.Id);
            details.Add(new StockTransferDetail
            {
                ProductId = originProduct.Id,
                Quantity = line.Quantity,
                QuantityReceived = 0,
                UnitCost = originProduct.CostPrice,
            });
        }

        var entity = new StockTransfer
        {
            TransferNumber = await _repo.GenerateTransferNumberAsync(companyId),
            FromWarehouseId = fromWarehouse.Id,
            ToWarehouseId = toWarehouse.Id,
            FromBranchId = fromWarehouse.BranchId,
            ToBranchId = toWarehouse.BranchId,
            Status = "draft",
            Notes = request.Notes,
            Details = details,
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(entity.TenantId, "stock_transfer.created",
            new { StockTransferId = entity.Id, TransferNumber = entity.TransferNumber });

        return MapToResponse(entity);
    }

    /// <summary>
    /// Despacha: descuenta stock en la bodega origen y registra el movimiento
    /// "transfer_out" de cada línea (congela UnitCost). Pasa a in_transit.
    /// </summary>
    public async Task<StockTransferResponse> DispatchAsync(Guid id)
    {
        var transfer = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Transferencia no encontrada");
        if (transfer.Status != "draft")
            throw new InvalidOperationException(
                $"Solo se puede despachar una transferencia en borrador (estado actual: {transfer.Status}).");

        foreach (var detail in transfer.Details)
        {
            var product = await _productRepo.GetByIdForUpdateAsync(detail.ProductId)
                ?? throw new InvalidOperationException($"Producto no disponible: {detail.ProductId}");

            if (product.Stock < detail.Quantity)
                throw new InvalidOperationException(
                    $"Stock insuficiente para '{product.Name}': disponible {product.Stock}, solicitado {detail.Quantity}.");

            detail.UnitCost = product.CostPrice;
            var stockBefore = product.Stock;
            product.Stock -= detail.Quantity;

            await _movementRepo.AddAsync(new InventoryMovement
            {
                ProductId = product.Id,
                MovementType = "transfer_out",
                Quantity = detail.Quantity,
                StockBefore = stockBefore,
                StockAfter = product.Stock,
                UnitCost = detail.UnitCost,
                ReferenceNumber = transfer.TransferNumber,
                Notes = $"Transferencia {transfer.TransferNumber} hacia {transfer.ToWarehouse?.Name ?? transfer.ToWarehouseId.ToString()} ({transfer.ToBranch?.Name ?? transfer.ToBranchId.ToString()})",
                BranchId = transfer.FromBranchId,
            });
        }

        transfer.Status = "in_transit";
        transfer.DispatchedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(transfer.TenantId, "stock_transfer.dispatched",
            new { StockTransferId = transfer.Id, TransferNumber = transfer.TransferNumber });

        return MapToResponse(transfer);
    }

    /// <summary>
    /// Recepción total (<paramref name="request"/> null u omitido) o parcial
    /// (líneas con cantidades). Cada línea acredita en la bodega destino
    /// (promedio ponderado si el producto ya existe allí, clon del catálogo si
    /// no) y registra "transfer_in". Al completar todas las líneas → completed;
    /// si queda pendiente → partially_received (re-receivable).
    /// </summary>
    public async Task<StockTransferResponse> ReceiveAsync(
        Guid id, ReceiveStockTransferRequest? request = null)
    {
        var transfer = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Transferencia no encontrada");
        if (transfer.Status is not ("in_transit" or "partially_received"))
            throw new InvalidOperationException(
                $"Solo se puede recibir una transferencia en tránsito (estado actual: {transfer.Status}).");

        var lines = BuildReceiveLines(transfer, request);

        foreach (var (detail, quantity) in lines)
        {
            var originProduct = detail.Product
                ?? await _productRepo.GetByIdAsync(detail.ProductId)
                ?? throw new InvalidOperationException($"Producto no disponible: {detail.ProductId}");

            var destination = await _productRepo.GetByCodeForWarehouseAsync(
                originProduct.Code, transfer.ToWarehouseId);

            if (destination is not null)
            {
                var stockBefore = destination.Stock;
                var totalStockValue = (destination.CostPrice * destination.Stock) +
                                      (detail.UnitCost * quantity);
                destination.Stock += quantity;
                destination.CostPrice = totalStockValue / destination.Stock;

                await _movementRepo.AddAsync(new InventoryMovement
                {
                    ProductId = destination.Id,
                    MovementType = "transfer_in",
                    Quantity = quantity,
                    StockBefore = stockBefore,
                    StockAfter = destination.Stock,
                    UnitCost = detail.UnitCost,
                    ReferenceNumber = transfer.TransferNumber,
                    Notes = $"Transferencia {transfer.TransferNumber} desde {transfer.FromWarehouse?.Name ?? transfer.FromWarehouseId.ToString()} ({transfer.FromBranch?.Name ?? transfer.FromBranchId.ToString()})",
                    BranchId = transfer.ToBranchId,
                });
            }
            else
            {
                var clone = new Product
                {
                    Code = originProduct.Code,
                    Name = originProduct.Name,
                    Description = originProduct.Description,
                    CategoryId = originProduct.CategoryId,
                    BrandId = originProduct.BrandId,
                    SupplierId = originProduct.SupplierId,
                    CostPrice = detail.UnitCost,
                    SellingPrice = originProduct.SellingPrice,
                    UnitOfMeasure = originProduct.UnitOfMeasure,
                    Stock = quantity,
                    MinStock = originProduct.MinStock,
                    MaxStock = originProduct.MaxStock,
                    ImageUrl = originProduct.ImageUrl,
                    Barcode = originProduct.Barcode,
                    IsActive = true,
                    TaxCategoryId = originProduct.TaxCategoryId,
                    WarehouseId = transfer.ToWarehouseId,
                    BranchId = transfer.ToBranchId,
                    TenantId = originProduct.TenantId,
                    CompanyId = originProduct.CompanyId,
                    CreatedBy = originProduct.CreatedBy,
                };
                await _productRepo.AddAsync(clone);
                // El producto creado es el destino del movimiento (se fija tras Add).
                await _movementRepo.AddAsync(new InventoryMovement
                {
                    ProductId = clone.Id,
                    MovementType = "transfer_in",
                    Quantity = quantity,
                    StockBefore = 0,
                    StockAfter = quantity,
                    UnitCost = detail.UnitCost,
                    ReferenceNumber = transfer.TransferNumber,
                    Notes = $"Transferencia {transfer.TransferNumber} desde {transfer.FromWarehouse?.Name ?? transfer.FromWarehouseId.ToString()} ({transfer.FromBranch?.Name ?? transfer.FromBranchId.ToString()})",
                    BranchId = transfer.ToBranchId,
                });
            }

            detail.QuantityReceived += quantity;
        }

        var fullyReceived = transfer.Details.All(d => d.QuantityReceived >= d.Quantity);
        transfer.Status = fullyReceived ? "completed" : "partially_received";
        transfer.ReceivedAt ??= DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(transfer.TenantId,
            fullyReceived ? "stock_transfer.completed" : "stock_transfer.partially_received",
            new { StockTransferId = transfer.Id, TransferNumber = transfer.TransferNumber });

        return MapToResponse(transfer);
    }

    public async Task<StockTransferResponse> CancelAsync(Guid id)
    {
        var transfer = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Transferencia no encontrada");
        if (transfer.Status != "draft")
            throw new InvalidOperationException(
                $"Solo se puede cancelar una transferencia en borrador (estado actual: {transfer.Status}).");

        transfer.Status = "cancelled";
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(transfer.TenantId, "stock_transfer.cancelled",
            new { StockTransferId = transfer.Id, TransferNumber = transfer.TransferNumber });

        return MapToResponse(transfer);
    }

    public async Task<StockTransferResponse?> GetByIdAsync(Guid id)
    {
        var transfer = await _repo.GetByIdAsync(id);
        return transfer is null ? null : MapToResponse(transfer);
    }

    public async Task<(List<StockTransferResponse> Items, int Total)> GetFilteredAsync(
        string? search, string? status, Guid? branchId, Guid? warehouseId, int page, int pageSize)
    {
        var effectiveBranchId = branchId ?? _tenant.ResolveBranchId() ?? Guid.Empty;
        var effectiveWarehouseId = warehouseId ?? _tenant.ResolveWarehouseId() ?? Guid.Empty;
        var (items, total) = await _repo.GetFilteredAsync(
            search, status, effectiveBranchId, effectiveWarehouseId, page, pageSize);
        return (items.Select(MapToResponse).ToList(), total);
    }

    /// <summary>
    /// Resuelve la bodega ORIGEN del producto REAL. El catálogo es por bodega
    /// ((Code, WarehouseId) único): si el productId enviado pertenece a otra
    /// bodega (p. ej. vino del catálogo local offline), se remapea por código.
    /// </summary>
    private async Task<Product> ResolveOriginProductAsync(Guid productId, Guid fromWarehouseId)
    {
        var product = await _productRepo.GetByIdAsync(productId)
            ?? throw new InvalidOperationException($"Producto no encontrado: {productId}");

        if (product.WarehouseId == fromWarehouseId)
            return product;

        var inOrigin = await _productRepo.GetByCodeForWarehouseAsync(product.Code, fromWarehouseId)
            ?? throw new InvalidOperationException(
                $"El producto '{product.Name}' no existe en la bodega origen.");

        return inOrigin;
    }

    /// <summary>
    /// Bodega explícita → validada; solo sucursal → su bodega por defecto;
    /// origen sin nada → bodega activa del contexto (cabecera).
    /// </summary>
    private async Task<Warehouse> ResolveWarehouseAsync(
        Guid? warehouseId, Guid? branchId, string label, bool allowContextFallback)
    {
        if (warehouseId is { } w && w != Guid.Empty)
        {
            return await _warehouseRepo.GetByIdAsync(w)
                ?? throw new InvalidOperationException($"La bodega {label} no existe.");
        }

        Guid? effectiveBranchId = null;
        if (branchId is { } b && b != Guid.Empty)
        {
            // Validación de pertenencia a la compañía operativa (patrón BranchValidator).
            effectiveBranchId = await _branchValidator.ResolveForWriteAsync(b) ?? b;
        }
        else if (allowContextFallback)
        {
            effectiveBranchId = _tenant.ResolveBranchId();
        }

        if (effectiveBranchId is { } branch)
        {
            var def = await _warehouseRepo.GetDefaultAsync(branch);
            if (def is not null) return def;

            var all = await _warehouseRepo.GetByBranchAsync(branch);
            var first = all.FirstOrDefault(x => x.IsActive) ?? all.FirstOrDefault();
            if (first is not null) return first;
        }

        if (allowContextFallback && _tenant.ResolveWarehouseId() is { } contextWarehouse)
        {
            return await _warehouseRepo.GetByIdAsync(contextWarehouse)
                ?? throw new InvalidOperationException("La bodega activa del contexto no existe.");
        }

        throw new InvalidOperationException(
            $"Debe indicar la bodega {label} (o una sucursal con bodegas configuradas).");
    }

    /// <summary>
    /// Líneas a recibir: total (request null o Lines null) o las indicadas,
    /// validando cantidades contra el pendiente de cada línea.
    /// </summary>
    private static List<(StockTransferDetail Detail, int Quantity)> BuildReceiveLines(
        StockTransfer transfer, ReceiveStockTransferRequest? request)
    {
        if (request?.Lines is null)
        {
            return transfer.Details
                .Select(d => (d, d.Quantity - d.QuantityReceived))
                .Where(x => x.Item2 > 0)
                .ToList();
        }

        if (request.Lines.Count == 0)
            throw new InvalidOperationException("Debe indicar al menos un producto por recibir.");

        var lines = new List<(StockTransferDetail, int)>(request.Lines.Count);
        foreach (var line in request.Lines)
        {
            var detail = transfer.Details.FirstOrDefault(d => d.Id == line.DetailId)
                ?? throw new InvalidOperationException($"Línea de transferencia no válida: {line.DetailId}");

            if (line.Quantity <= 0)
                throw new InvalidOperationException("Todas las cantidades deben ser mayores a cero.");

            var pending = detail.Quantity - detail.QuantityReceived;
            if (pending <= 0)
                throw new InvalidOperationException(
                    $"La línea '{detail.Product?.Name ?? detail.ProductId.ToString()}' ya fue recibida en su totalidad.");

            if (line.Quantity > pending)
                throw new InvalidOperationException(
                    $"La cantidad a recibir ({line.Quantity}) excede el pendiente ({pending}) para '{detail.Product?.Name ?? detail.ProductId.ToString()}'.");

            lines.Add((detail, line.Quantity));
        }

        return lines;
    }

    private Guid RequireCompanyId()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId) || companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant inválido");
        return companyId;
    }

    private static StockTransferResponse MapToResponse(StockTransfer t) => new(
        t.Id, t.TransferNumber,
        t.FromWarehouseId, t.FromWarehouse?.Name,
        t.ToWarehouseId, t.ToWarehouse?.Name,
        t.FromBranchId, t.FromBranch?.Name,
        t.ToBranchId, t.ToBranch?.Name,
        t.Status, t.DispatchedAt, t.ReceivedAt, t.Notes,
        t.CreatedBy, t.CreatedAt,
        t.Details.Select(d => new StockTransferDetailResponse(
            d.Id, d.ProductId, d.Product?.Name, d.Product?.Code,
            d.Quantity, d.QuantityReceived, d.UnitCost)).ToList());
}
