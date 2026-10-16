using Zorvian.Application.DTOs.StockTransfer;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

/// <summary>
/// Transferencia de stock entre bodegas (sucursales) de la misma compañía.
/// Máquina de estados: draft → in_transit (despacho: baja + movimiento en ORIGEN)
/// → completed (recepción: alta + movimiento en DESTINO, con clon del producto si
/// la sucursal destino aún no lo maneja); cancelled solo desde draft.
/// </summary>
public sealed class StockTransferService
{
    private readonly IStockTransferRepository _repo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly IBranchValidator _branchValidator;
    private readonly IWebhookService _webhook;
    private readonly ITenantContext _tenant;

    public StockTransferService(
        IStockTransferRepository repo,
        IProductRepository productRepo,
        IInventoryMovementRepository movementRepo,
        IBranchValidator branchValidator,
        IWebhookService webhook,
        ITenantContext tenant)
    {
        _repo = repo;
        _productRepo = productRepo;
        _movementRepo = movementRepo;
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

        var fromBranch = await _branchValidator.ResolveForWriteAsync(request.FromBranchId);
        var toBranch = await _branchValidator.ResolveForWriteAsync(request.ToBranchId);
        if (fromBranch is null)
            throw new InvalidOperationException("Debe indicar la sucursal origen.");
        if (toBranch is null)
            throw new InvalidOperationException("Debe indicar la sucursal destino.");
        if (fromBranch == toBranch)
            throw new InvalidOperationException("La sucursal origen y destino deben ser distintas.");

        var companyId = RequireCompanyId();

        var details = new List<StockTransferDetail>(request.Details.Count);
        foreach (var line in request.Details)
        {
            var originProduct = await ResolveOriginProductAsync(line.ProductId, fromBranch.Value);
            details.Add(new StockTransferDetail
            {
                ProductId = originProduct.Id,
                Quantity = line.Quantity,
                UnitCost = originProduct.CostPrice,
            });
        }

        var entity = new StockTransfer
        {
            TransferNumber = await _repo.GenerateTransferNumberAsync(companyId),
            FromBranchId = fromBranch.Value,
            ToBranchId = toBranch.Value,
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
    /// Despacha: descuenta stock en la sucursal origen y registra el movimiento
    /// "transfer_out" de cada línea. Pasa la transferencia a in_transit.
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
                Notes = $"Transferencia {transfer.TransferNumber} hacia {transfer.ToBranch?.Name ?? transfer.ToBranchId.ToString()}",
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
    /// Recepción: da de alta el stock en la sucursal destino (promedio ponderado si
    /// el producto ya existe allí, clon del catálogo si no) y registra "transfer_in".
    /// </summary>
    public async Task<StockTransferResponse> ReceiveAsync(Guid id)
    {
        var transfer = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Transferencia no encontrada");
        if (transfer.Status != "in_transit")
            throw new InvalidOperationException(
                $"Solo se puede recibir una transferencia en tránsito (estado actual: {transfer.Status}).");

        foreach (var detail in transfer.Details)
        {
            var originProduct = await _productRepo.GetByIdAsync(detail.ProductId)
                ?? throw new InvalidOperationException($"Producto no disponible: {detail.ProductId}");

            var destination = await _productRepo.GetByCodeAsync(originProduct.Code, transfer.ToBranchId);
            if (destination is not null)
            {
                var totalStockValue = (destination.CostPrice * destination.Stock) +
                                      (detail.UnitCost * detail.Quantity);
                var stockBefore = destination.Stock;
                destination.Stock += detail.Quantity;
                destination.CostPrice = totalStockValue / destination.Stock;

                await _movementRepo.AddAsync(BuildIncomingMovement(
                    transfer, destination.Id, detail, stockBefore, destination.Stock));
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
                    Stock = detail.Quantity,
                    MinStock = originProduct.MinStock,
                    MaxStock = originProduct.MaxStock,
                    ImageUrl = originProduct.ImageUrl,
                    Barcode = originProduct.Barcode,
                    IsActive = true,
                    TaxCategoryId = originProduct.TaxCategoryId,
                    BranchId = transfer.ToBranchId,
                    TenantId = originProduct.TenantId,
                    CompanyId = originProduct.CompanyId,
                    CreatedBy = originProduct.CreatedBy,
                };
                await _productRepo.AddAsync(clone);

                await _movementRepo.AddAsync(BuildIncomingMovement(
                    transfer, clone.Id, detail, 0, detail.Quantity));
            }
        }

        transfer.Status = "completed";
        transfer.ReceivedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(transfer.TenantId, "stock_transfer.completed",
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
        string? search, string? status, Guid? branchId, int page, int pageSize)
    {
        var effectiveBranchId = branchId ?? _tenant.ResolveBranchId() ?? Guid.Empty;
        var (items, total) = await _repo.GetFilteredAsync(
            search, status, effectiveBranchId, page, pageSize);
        return (items.Select(MapToResponse).ToList(), total);
    }

    /// <summary>
    /// Resuelve el producto REAL de la sucursal origen. El catálogo es por sucursal
    /// ((Code, BranchId) único): si el productId enviado pertenece a otra sucursal
    /// (p. ej. vino del catálogo local offline), se remapea por código.
    /// </summary>
    private async Task<Product> ResolveOriginProductAsync(Guid productId, Guid fromBranchId)
    {
        var product = await _productRepo.GetByIdAsync(productId)
            ?? throw new InvalidOperationException($"Producto no encontrado: {productId}");

        if (product.BranchId == fromBranchId)
            return product;

        var inOrigin = await _productRepo.GetByCodeAsync(product.Code, fromBranchId)
            ?? throw new InvalidOperationException(
                $"El producto '{product.Name}' no existe en la sucursal origen.");

        return inOrigin;
    }

    private static InventoryMovement BuildIncomingMovement(
        StockTransfer transfer, Guid productId, StockTransferDetail detail,
        int stockBefore, int stockAfter) => new()
    {
        ProductId = productId,
        MovementType = "transfer_in",
        Quantity = detail.Quantity,
        StockBefore = stockBefore,
        StockAfter = stockAfter,
        UnitCost = detail.UnitCost,
        ReferenceNumber = transfer.TransferNumber,
        Notes = $"Transferencia {transfer.TransferNumber} desde {transfer.FromBranch?.Name ?? transfer.FromBranchId.ToString()}",
        BranchId = transfer.ToBranchId,
    };

    private Guid RequireCompanyId()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId) || companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant inválido");
        return companyId;
    }

    private static StockTransferResponse MapToResponse(StockTransfer t) => new(
        t.Id, t.TransferNumber,
        t.FromBranchId, t.FromBranch?.Name,
        t.ToBranchId, t.ToBranch?.Name,
        t.Status, t.DispatchedAt, t.ReceivedAt, t.Notes,
        t.CreatedBy, t.CreatedAt,
        t.Details.Select(d => new StockTransferDetailResponse(
            d.ProductId, d.Product?.Name, d.Product?.Code,
            d.Quantity, d.UnitCost)).ToList());
}
