using AutoMapper;
using Zorvian.Application.DTOs.PurchaseOrder;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class PurchaseOrderService
{
    private readonly IPurchaseOrderRepository _poRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly IWebhookService _webhook;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public PurchaseOrderService(
        IPurchaseOrderRepository poRepo,
        IPurchaseRepository purchaseRepo,
        IProductRepository productRepo,
        IInventoryMovementRepository movementRepo,
        ICompanyRepository companyRepo,
        ISupplierRepository supplierRepo,
        IAutoAccountingService autoAccounting,
        IWebhookService webhook,
        ITenantContext tenant,
        IMapper mapper)
    {
        _poRepo = poRepo;
        _purchaseRepo = purchaseRepo;
        _productRepo = productRepo;
        _movementRepo = movementRepo;
        _companyRepo = companyRepo;
        _supplierRepo = supplierRepo;
        _autoAccounting = autoAccounting;
        _webhook = webhook;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request)
    {
        var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId)
            ?? throw new InvalidOperationException("Supplier not found");

        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Invalid tenant");

        var company = await _companyRepo.GetByIdAsync(companyId)
            ?? throw new InvalidOperationException("Company not found");
        var settings = await _companyRepo.GetSettingsAsync(companyId);
        var defaultTaxRate = settings?.TaxRate ?? 0.15m;

        decimal subtotal = 0;
        decimal totalTax = 0;

        foreach (var line in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(line.ProductId)
                ?? throw new InvalidOperationException($"Product not found: {line.ProductId}");
            var lineSubtotal = line.QuantityOrdered * line.UnitCost;
            subtotal += lineSubtotal;
            var rate = product.TaxCategory?.Rate ?? defaultTaxRate;
            totalTax += (lineSubtotal - line.Discount) * rate;
        }

        var total = (subtotal - request.Discount) + totalTax;

        var order = new PurchaseOrder
        {
            OrderNumber = await _poRepo.GenerateOrderNumberAsync(companyId),
            CountryCode = request.CountryCode ?? company.Country switch
            {
                "Nicaragua" => "NIC",
                "Costa Rica" => "CRI",
                "El Salvador" => "SLV",
                "Honduras" => "HND",
                "Guatemala" => "GTM",
                _ => "NIC"
            },
            SupplierId = request.SupplierId,
            OrderDate = request.OrderDate,
            ExpectedDate = request.ExpectedDate,
            Status = "draft",
            Subtotal = subtotal,
            Tax = totalTax,
            Discount = request.Discount,
            Total = total,
            Notes = request.Notes,
            BranchId = request.BranchId,
            CurrencyCode = request.CurrencyCode,
            Details = request.Details.Select(d => new PurchaseOrderDetail
            {
                ProductId = d.ProductId,
                QuantityOrdered = d.QuantityOrdered,
                UnitCost = d.UnitCost,
                Discount = d.Discount,
                Subtotal = d.QuantityOrdered * d.UnitCost - d.Discount,
                BranchId = request.BranchId,
            }).ToList(),
        };

        await _poRepo.AddAsync(order);
        await _poRepo.SaveChangesAsync();
        await _webhook.PublishAsync(order.TenantId, "purchase_order.created",
            new { PurchaseOrderId = order.Id, OrderNumber = order.OrderNumber });

        return await GetByIdAsync(order.Id)
            ?? throw new InvalidOperationException("Failed to create purchase order");
    }

    public async Task<PurchaseOrderResponse> ApproveAsync(Guid id)
    {
        var order = await _poRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Purchase order not found");

        if (order.Status != "draft" && order.Status != "pending_approval")
            throw new InvalidOperationException($"Cannot approve order with status '{order.Status}'");

        order.Status = "approved";
        await _poRepo.SaveChangesAsync();
        await _webhook.PublishAsync(order.TenantId, "purchase_order.approved",
            new { PurchaseOrderId = order.Id, OrderNumber = order.OrderNumber });

        return (await GetByIdAsync(order.Id))!;
    }

    public async Task<PurchaseOrderResponse> CancelAsync(Guid id)
    {
        var order = await _poRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Purchase order not found");

        if (order.Status == "completed" || order.Status == "cancelled")
            throw new InvalidOperationException($"Cannot cancel order with status '{order.Status}'");

        order.Status = "cancelled";
        await _poRepo.SaveChangesAsync();
        await _webhook.PublishAsync(order.TenantId, "purchase_order.cancelled",
            new { PurchaseOrderId = order.Id, OrderNumber = order.OrderNumber });

        return (await GetByIdAsync(order.Id))!;
    }

    public async Task<PurchaseOrderResponse> ReceiveAsync(ReceivePurchaseOrderRequest request)
    {
        var order = await _poRepo.GetByIdAsync(request.PurchaseOrderId)
            ?? throw new KeyNotFoundException("Purchase order not found");

        if (order.Status != "approved" && order.Status != "partially_received")
            throw new InvalidOperationException($"Cannot receive order with status '{order.Status}'");

        var companyId = order.CompanyId;
        var settings = await _companyRepo.GetSettingsAsync(companyId);
        var defaultTaxRate = settings?.TaxRate ?? 0.15m;

        // Validate lines belong to the order
        foreach (var line in request.Lines)
        {
            var detail = order.Details.FirstOrDefault(d => d.ProductId == line.ProductId)
                ?? throw new InvalidOperationException($"Product {line.ProductId} not in this purchase order");
            var newReceived = detail.QuantityReceived + line.QuantityReceived;
            if (newReceived > detail.QuantityOrdered)
                throw new InvalidOperationException(
                    $"Cannot receive more than ordered for product {detail.Product?.Name ?? line.ProductId.ToString()}. " +
                    $"Ordered: {detail.QuantityOrdered}, Already received: {detail.QuantityReceived}, Attempting: {newReceived}");
        }

        // Build purchase request from PO lines being received
        decimal subtotal = 0;
        decimal totalTax = 0;

        foreach (var line in request.Lines)
        {
            var lineSubtotal = line.QuantityReceived * line.UnitCost;
            subtotal += lineSubtotal;
            var product = await _productRepo.GetByIdAsync(line.ProductId);
            var rate = product?.TaxCategory?.Rate ?? defaultTaxRate;
            totalTax += lineSubtotal * rate;
        }

        var purchaseTotal = (subtotal - order.Discount) + totalTax;

        var purchase = new Purchase
        {
            PurchaseNumber = await _purchaseRepo.GeneratePurchaseNumberAsync(companyId),
            SupplierId = order.SupplierId,
            PurchaseDate = DateTime.UtcNow,
            Status = "pending",
            Subtotal = subtotal,
            Tax = totalTax,
            Discount = order.Discount,
            Total = purchaseTotal,
            PaidAmount = 0,
            Balance = purchaseTotal,
            Notes = $"Recibido de OC {order.OrderNumber}",
            BranchId = order.BranchId,
            CurrencyCode = order.CurrencyCode,
            CountryCode = order.CountryCode,
            PurchaseOrderId = order.Id,
            Details = request.Lines.Select(l => new PurchaseDetail
            {
                ProductId = l.ProductId,
                Quantity = l.QuantityReceived,
                UnitCost = l.UnitCost,
                Discount = 0,
                Subtotal = l.QuantityReceived * l.UnitCost,
                BranchId = order.BranchId,
            }).ToList(),
        };

        await _purchaseRepo.AddAsync(purchase);
        await _purchaseRepo.SaveChangesAsync();

        // Update inventory for each received line
        foreach (var line in request.Lines)
        {
            var product = await _productRepo.GetByIdAsync(line.ProductId);
            if (product is null) continue;

            var totalStockValue = (product.CostPrice * product.Stock) + (line.UnitCost * line.QuantityReceived);
            product.Stock += line.QuantityReceived;
            product.CostPrice = totalStockValue / product.Stock;

            var movement = new InventoryMovement
            {
                ProductId = line.ProductId,
                MovementType = "purchase",
                Quantity = line.QuantityReceived,
                StockBefore = product.Stock - line.QuantityReceived,
                StockAfter = product.Stock,
                UnitCost = line.UnitCost,
                ReferenceNumber = purchase.PurchaseNumber,
                BranchId = order.BranchId,
            };
            await _movementRepo.AddAsync(movement);

            // Update received quantity in PO detail
            var detail = order.Details.First(d => d.ProductId == line.ProductId);
            detail.QuantityReceived += line.QuantityReceived;
        }

        // Update PO status
        var allFullyReceived = order.Details.All(d => d.QuantityReceived >= d.QuantityOrdered);
        order.Status = allFullyReceived ? "completed" : "partially_received";
        order.PurchaseId = purchase.Id;

        await _poRepo.SaveChangesAsync();

        // Generate accounting entry
        await _autoAccounting.GeneratePurchaseEntryAsync(
            purchase.Id, purchase.Details.ToList(), purchase.Discount, purchase.Total, order.CountryCode);

        await _webhook.PublishAsync(order.TenantId, "purchase_order.received",
            new { PurchaseOrderId = order.Id, PurchaseId = purchase.Id, PurchaseNumber = purchase.PurchaseNumber });

        return (await GetByIdAsync(order.Id))!;
    }

    public async Task<PurchaseOrderResponse?> GetByIdAsync(Guid id)
    {
        var order = await _poRepo.GetByIdAsync(id);
        if (order == null) return null;
        return MapToResponse(order);
    }

    public async Task<(List<PurchaseOrderResponse> Items, int Total)> GetFilteredAsync(
        string? search, Guid? supplierId, string? status,
        DateTime? fromDate, DateTime? toDate, Guid branchId,
        int page, int pageSize)
    {
        var (items, total) = await _poRepo.GetFilteredAsync(
            search, supplierId, status, fromDate, toDate, branchId, page, pageSize);
        var responses = items.Select(MapToResponse).ToList();
        return (responses, total);
    }

    private PurchaseOrderResponse MapToResponse(PurchaseOrder o)
    {
        return new PurchaseOrderResponse(
            o.Id, o.OrderNumber, o.SupplierId,
            o.Supplier?.Name,
            o.OrderDate, o.ExpectedDate, o.Status,
            o.Subtotal, o.Tax, o.Discount, o.Total,
            o.Notes, o.BranchId, o.CurrencyCode, o.CountryCode,
            o.PurchaseId, null,
            o.CreatedAt,
            o.Details.Select(d => new PurchaseOrderDetailResponse(
                d.ProductId, d.Product?.Name, d.Product?.Code,
                d.QuantityOrdered, d.QuantityReceived,
                d.UnitCost, d.Discount, d.Subtotal
            )).ToList()
        );
    }
}
