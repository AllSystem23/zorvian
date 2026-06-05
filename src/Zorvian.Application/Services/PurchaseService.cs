using AutoMapper;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class PurchaseService
{
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public PurchaseService(
        IPurchaseRepository purchaseRepo,
        IProductRepository productRepo,
        IInventoryMovementRepository movementRepo,
        ICompanyRepository companyRepo,
        IAutoAccountingService autoAccounting,
        ITenantContext tenant,
        IMapper mapper)
    {
        _purchaseRepo = purchaseRepo;
        _productRepo = productRepo;
        _movementRepo = movementRepo;
        _companyRepo = companyRepo;
        _autoAccounting = autoAccounting;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<PurchaseResponse> CreateAsync(CreatePurchaseRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Invalid tenant");

        decimal subtotal = 0;
        decimal totalTax = 0;

        foreach (var detail in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(detail.ProductId)
                ?? throw new InvalidOperationException($"Product not found: {detail.ProductId}");

            var lineSubtotal = detail.Quantity * detail.UnitPrice;
            subtotal += lineSubtotal;
            var rate = product.TaxCategory?.Rate ?? 0m;
            totalTax += (lineSubtotal - detail.Discount) * rate;
        }

        var total = (subtotal - request.Discount) + totalTax;

        var purchase = new Purchase
        {
            PurchaseNumber = await _purchaseRepo.GeneratePurchaseNumberAsync(companyId),
            SupplierId = request.SupplierId,
            PurchaseDate = request.PurchaseDate,
            InvoiceReference = request.InvoiceReference,
            Status = "completed",
            Subtotal = subtotal,
            Tax = totalTax,
            Discount = request.Discount,
            Total = total,
            Notes = request.Notes,
            CompanyId = companyId,
            BranchId = request.BranchId,
            Details = request.Details.Select(d => new PurchaseDetail
            {
                ProductId = d.ProductId,
                Quantity = d.Quantity,
                UnitCost = d.UnitPrice,
                Discount = d.Discount,
                Subtotal = d.Quantity * d.UnitPrice - d.Discount,
                CompanyId = companyId,
                BranchId = request.BranchId,
            }).ToList(),
        };

        await _purchaseRepo.AddAsync(purchase);

        foreach (var detail in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(detail.ProductId);
            if (product is null) continue;

            // Average Cost Update
            var totalStockValue = (product.CostPrice * product.Stock) + (detail.UnitPrice * detail.Quantity);
            product.Stock += detail.Quantity;
            product.CostPrice = totalStockValue / product.Stock;

            var movement = new InventoryMovement
            {
                ProductId = detail.ProductId,
                MovementType = "purchase",
                Quantity = detail.Quantity,
                StockBefore = product.Stock - detail.Quantity,
                StockAfter = product.Stock,
                UnitCost = detail.UnitPrice,
                ReferenceNumber = purchase.PurchaseNumber,
                CompanyId = companyId,
                BranchId = request.BranchId,
            };
            await _movementRepo.AddAsync(movement);
        }

        await _purchaseRepo.SaveChangesAsync();

        await _autoAccounting.GeneratePurchaseEntryAsync(
            purchase.Id, purchase.Details.ToList(), purchase.Discount, purchase.Total);

        return await GetByIdAsync(purchase.Id) ?? throw new InvalidOperationException("Failed to create purchase");
    }

    public async Task<PurchaseResponse> UpdateAsync(Guid id, UpdatePurchaseRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Invalid tenant");

        var purchase = await _purchaseRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Purchase not found");

        if (purchase.Status == "cancelled")
            throw new InvalidOperationException("Cannot update a cancelled purchase");

        // Revert old stock (simplified for now, ideally should reverse average cost calculation)
        foreach (var oldDetail in purchase.Details)
        {
            var product = await _productRepo.GetByIdAsync(oldDetail.ProductId);
            if (product is null) continue;
            product.Stock -= oldDetail.Quantity;
        }

        // Remove old details from DB via explicit loading
        purchase.Details.Clear();

        // Apply new values
        if (request.SupplierId.HasValue)
            purchase.SupplierId = request.SupplierId.Value;
        if (request.PurchaseDate.HasValue)
            purchase.PurchaseDate = request.PurchaseDate.Value;
        if (request.InvoiceReference != null)
            purchase.InvoiceReference = request.InvoiceReference;
        if (request.Discount.HasValue)
            purchase.Discount = request.Discount.Value;
        if (request.Notes != null)
            purchase.Notes = request.Notes;
        purchase.Status = "completed";

        if (request.Details != null && request.Details.Count > 0)
        {
            decimal subtotal = 0;
            decimal totalTax = 0;

            foreach (var detail in request.Details)
            {
                var product = await _productRepo.GetByIdAsync(detail.ProductId)
                    ?? throw new InvalidOperationException($"Product not found: {detail.ProductId}");

                var lineSubtotal = detail.Quantity * detail.UnitPrice;
                subtotal += lineSubtotal;
                var rate = product.TaxCategory?.Rate ?? 0m;
                totalTax += (lineSubtotal - detail.Discount) * rate;
            }

            var total = (subtotal - purchase.Discount) + totalTax;

            purchase.Subtotal = subtotal;
            purchase.Tax = totalTax;
            purchase.Total = total;

            purchase.Details = request.Details.Select(d => new PurchaseDetail
            {
                PurchaseId = purchase.Id,
                ProductId = d.ProductId,
                Quantity = d.Quantity,
                UnitCost = d.UnitPrice,
                Discount = d.Discount,
                Subtotal = d.Quantity * d.UnitPrice - d.Discount,
                CompanyId = companyId,
                BranchId = purchase.BranchId,
            }).ToList();

            // Apply new stock + movements
            foreach (var detail in request.Details)
            {
                var product = await _productRepo.GetByIdAsync(detail.ProductId);
                if (product is null) continue;

                var stockBefore = product.Stock;
                product.Stock += detail.Quantity;

                var movement = new InventoryMovement
                {
                    ProductId = detail.ProductId,
                    MovementType = "purchase",
                    Quantity = detail.Quantity,
                    StockBefore = stockBefore,
                    StockAfter = product.Stock,
                    UnitCost = detail.UnitPrice,
                    ReferenceNumber = purchase.PurchaseNumber,
                    CompanyId = companyId,
                    BranchId = purchase.BranchId,
                };
                await _movementRepo.AddAsync(movement);
            }
        }

        await _purchaseRepo.UpdateAsync(purchase);
        await _purchaseRepo.SaveChangesAsync();

        return await GetByIdAsync(purchase.Id) ?? throw new InvalidOperationException("Failed to update purchase");
    }

    public async Task<PurchaseResponse> CancelAsync(Guid id)
    {
        var purchase = await _purchaseRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Purchase not found");

        if (purchase.Status == "cancelled")
            throw new InvalidOperationException("Purchase is already cancelled");

        purchase.Status = "cancelled";

        // Revert stock
        foreach (var detail in purchase.Details)
        {
            var product = await _productRepo.GetByIdAsync(detail.ProductId);
            if (product is null) continue;

            var stockBefore = product.Stock;
            product.Stock -= detail.Quantity;

            var movement = new InventoryMovement
            {
                ProductId = detail.ProductId,
                MovementType = "purchase_cancellation",
                Quantity = detail.Quantity,
                StockBefore = stockBefore,
                StockAfter = product.Stock,
                UnitCost = detail.UnitCost,
                ReferenceNumber = purchase.PurchaseNumber,
                CompanyId = purchase.CompanyId,
                BranchId = purchase.BranchId,
            };
            await _movementRepo.AddAsync(movement);
        }

        await _purchaseRepo.UpdateAsync(purchase);
        await _purchaseRepo.SaveChangesAsync();

        // Need to add logic to generate reversal accounting entry if required by policy
        // Currently skipping to maintain current architectural scope based on instructions

        return await GetByIdAsync(purchase.Id) ?? throw new InvalidOperationException("Failed to cancel purchase");
    }

    public async Task<PurchaseResponse?> GetByIdAsync(Guid id)
    {
        var purchase = await _purchaseRepo.GetByIdAsync(id);
        if (purchase is null) return null;

        return new PurchaseResponse(
            purchase.Id,
            purchase.PurchaseNumber,
            purchase.SupplierId,
            purchase.Supplier?.Name ?? "",
            purchase.CreatedAt,
            purchase.PurchaseDate,
            purchase.InvoiceReference,
            purchase.Status,
            purchase.Subtotal,
            purchase.Tax,
            purchase.Discount,
            purchase.Total,
            purchase.Notes,
            purchase.Details.Select(d => new PurchaseDetailItem(
                d.ProductId,
                d.Product?.Name ?? "",
                d.Quantity,
                d.UnitCost,
                d.Discount,
                d.Subtotal
            )).ToList()
        );
    }

    public async Task<PagedResult<PurchaseListResponse>> GetFilteredAsync(PurchaseFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _purchaseRepo.GetFilteredAsync(filter.SupplierId, filter.Status, filter.FromDate, filter.ToDate, Guid.Empty, page, pageSize);
        var total = await _purchaseRepo.GetFilteredCountAsync(filter.SupplierId, filter.Status, filter.FromDate, filter.ToDate, Guid.Empty);

        return new PagedResult<PurchaseListResponse>(
            items.Select(p => new PurchaseListResponse(
                p.Id, p.PurchaseNumber, p.Supplier?.Name ?? "", p.CreatedAt, p.Status, p.Total
            )).ToList(),
            total, page, pageSize
        );
    }
}
