using AutoMapper;
using MassTransit;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Messages;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class PurchaseService
{
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly IWebhookService _webhook;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly IApprovalEngine _approvalEngine;
    private readonly IPublishEndpoint _publishEndpoint;

    public PurchaseService(
        IPurchaseRepository purchaseRepo,
        IProductRepository productRepo,
        IInventoryMovementRepository movementRepo,
        ICompanyRepository companyRepo,
        ISupplierRepository supplierRepo,
        IAutoAccountingService autoAccounting,
        IWebhookService webhook,
        ITenantContext tenant,
        IMapper mapper,
        IApprovalEngine approvalEngine,
        IPublishEndpoint publishEndpoint)
    {
        _purchaseRepo = purchaseRepo;
        _productRepo = productRepo;
        _movementRepo = movementRepo;
        _companyRepo = companyRepo;
        _supplierRepo = supplierRepo;
        _autoAccounting = autoAccounting;
        _webhook = webhook;
        _tenant = tenant;
        _mapper = mapper;
        _approvalEngine = approvalEngine;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<PurchaseResponse> CreateAsync(CreatePurchaseRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Invalid tenant");

        var company = await _companyRepo.GetByIdAsync(companyId)
            ?? throw new InvalidOperationException("Company not found");
        var settings = await _companyRepo.GetSettingsAsync(companyId);
        var defaultTaxRate = settings?.TaxRate ?? 0.15m;
        var countryCode = request.CountryCode ?? company.Country switch
        {
            "Nicaragua" => "NIC",
            "Costa Rica" => "CRI",
            "El Salvador" => "SLV",
            "Honduras" => "HND",
            "Guatemala" => "GTM",
            _ => "NIC"
        };

        var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId)
            ?? throw new InvalidOperationException("Supplier not found");

        decimal subtotal = 0;
        decimal totalTax = 0;

        foreach (var detail in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(detail.ProductId)
                ?? throw new InvalidOperationException($"Product not found: {detail.ProductId}");

            var lineSubtotal = detail.Quantity * detail.UnitCost;
            subtotal += lineSubtotal;
            var rate = product.TaxCategory?.Rate ?? defaultTaxRate;
            totalTax += (lineSubtotal - detail.Discount) * rate;
        }

        var total = (subtotal - request.Discount) + totalTax;

        var purchase = new Purchase
        {
            PurchaseNumber = await _purchaseRepo.GeneratePurchaseNumberAsync(companyId),
            SupplierId = request.SupplierId,
            PurchaseDate = request.PurchaseDate,
            DueDate = request.DueDate,
            InvoiceReference = request.InvoiceReference,
            Status = "pending",
            Subtotal = subtotal,
            Tax = totalTax,
            Discount = request.Discount,
            Total = total,
            PaidAmount = 0,
            Balance = total,
            Notes = request.Notes,
            BranchId = request.BranchId,
            CountryCode = countryCode,
            Details = request.Details.Select(d => new PurchaseDetail
            {
                ProductId = d.ProductId,
                Quantity = d.Quantity,
                UnitCost = d.UnitCost,
                Discount = d.Discount,
                Subtotal = d.Quantity * d.UnitCost - d.Discount,
                BranchId = request.BranchId,
            }).ToList(),
        };

        await _purchaseRepo.AddAsync(purchase);

        // Check if approval is needed
        var approvalResult = await _approvalEngine.EvaluateAsync(
            "Purchase", "Create", purchase.Id, total, _tenant.TenantId.ToString());
        if (approvalResult.RequiresApproval)
        {
            purchase.Status = "pending_approval";
            await _purchaseRepo.SaveChangesAsync();
            await _webhook.PublishAsync(purchase.TenantId, "purchase.pending_approval", new { PurchaseId = purchase.Id, PurchaseNumber = purchase.PurchaseNumber, Total = total });
            return await GetByIdAsync(purchase.Id) ?? throw new InvalidOperationException("Failed to create purchase");
        }

        await CompletePurchaseAsync(purchase, request);

        await _webhook.PublishAsync(purchase.TenantId, "purchase.created", new { PurchaseId = purchase.Id, PurchaseNumber = purchase.PurchaseNumber, Total = total });

        // Publish MassTransit event after all operations complete
        await _publishEndpoint.Publish(new PurchaseCreatedEvent
        {
            PurchaseId = purchase.Id,
            CompanyId = companyId,
            SupplierId = request.SupplierId,
            Total = purchase.Total,
            CurrencyCode = purchase.CurrencyCode ?? "NIO",
            PurchaseDate = DateTime.UtcNow,
            Items = purchase.Details.Select(d => new PurchaseItem
            {
                ProductId = d.ProductId,
                Quantity = d.Quantity,
                UnitCost = d.UnitCost,
                Subtotal = d.Subtotal,
            }).ToList(),
        });

        return await GetByIdAsync(purchase.Id) ?? throw new InvalidOperationException("Failed to create purchase");
    }

    private async Task CompletePurchaseAsync(Purchase purchase, CreatePurchaseRequest request)
    {
        var companyId = purchase.CompanyId;
        foreach (var detail in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(detail.ProductId);
            if (product is null) continue;

            var totalStockValue = (product.CostPrice * product.Stock) + (detail.UnitCost * detail.Quantity);
            product.Stock += detail.Quantity;
            product.CostPrice = totalStockValue / product.Stock;

            var movement = new InventoryMovement
            {
                ProductId = detail.ProductId,
                MovementType = "purchase",
                Quantity = detail.Quantity,
                StockBefore = product.Stock - detail.Quantity,
                StockAfter = product.Stock,
                UnitCost = detail.UnitCost,
                ReferenceNumber = purchase.PurchaseNumber,
                BranchId = request.BranchId,
            };
            await _movementRepo.AddAsync(movement);
        }

        await _purchaseRepo.SaveChangesAsync();

        await _autoAccounting.GeneratePurchaseEntryAsync(
            purchase.Id, purchase.Details.ToList(), purchase.Discount, purchase.Total, purchase.CountryCode);
    }

    public async Task<PurchaseResponse?> CompleteApprovedPurchaseAsync(Guid purchaseId)
    {
        var purchase = await _purchaseRepo.GetByIdAsync(purchaseId);
        if (purchase is null || purchase.Status != "pending_approval")
            return null;

        purchase.Status = "completed";
        await _purchaseRepo.SaveChangesAsync();

        await _autoAccounting.GeneratePurchaseEntryAsync(
            purchase.Id, purchase.Details.ToList(), purchase.Discount, purchase.Total, purchase.CountryCode);

        // Publish MassTransit event after approval completes
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            companyId = purchase.CompanyId;

        await _publishEndpoint.Publish(new PurchaseCreatedEvent
        {
            PurchaseId = purchase.Id,
            CompanyId = companyId,
            SupplierId = purchase.SupplierId,
            Total = purchase.Total,
            CurrencyCode = purchase.CurrencyCode ?? "NIO",
            PurchaseDate = DateTime.UtcNow,
            Items = purchase.Details.Select(d => new PurchaseItem
            {
                ProductId = d.ProductId,
                Quantity = d.Quantity,
                UnitCost = d.UnitCost,
                Subtotal = d.Subtotal,
            }).ToList(),
        });

        return await GetByIdAsync(purchase.Id);
    }

    public async Task<PurchaseResponse> UpdateAsync(Guid id, UpdatePurchaseRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Invalid tenant");

        var purchase = await _purchaseRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Purchase not found");

        if (purchase.Status == "cancelled")
            throw new InvalidOperationException("Cannot update a cancelled purchase");

        foreach (var oldDetail in purchase.Details)
        {
            var product = await _productRepo.GetByIdAsync(oldDetail.ProductId);
            if (product is null) continue;
            product.Stock -= oldDetail.Quantity;
        }

        purchase.Details.Clear();

        if (request.SupplierId.HasValue)
            purchase.SupplierId = request.SupplierId.Value;
        if (request.PurchaseDate.HasValue)
            purchase.PurchaseDate = request.PurchaseDate.Value;
        if (request.DueDate.HasValue)
            purchase.DueDate = request.DueDate.Value;
        if (request.InvoiceReference != null)
            purchase.InvoiceReference = request.InvoiceReference;
        if (request.Discount.HasValue)
            purchase.Discount = request.Discount.Value;
        if (request.Notes != null)
            purchase.Notes = request.Notes;
        purchase.Status = "pending";

        if (request.Details != null && request.Details.Count > 0)
        {
            var settings = await _companyRepo.GetSettingsAsync(companyId);
            var defaultTaxRate = settings?.TaxRate ?? 0.15m;

            decimal subtotal = 0;
            decimal totalTax = 0;

            foreach (var detail in request.Details)
            {
                var product = await _productRepo.GetByIdAsync(detail.ProductId)
                    ?? throw new InvalidOperationException($"Product not found: {detail.ProductId}");

                var lineSubtotal = detail.Quantity * detail.UnitCost;
                subtotal += lineSubtotal;
                var rate = product.TaxCategory?.Rate ?? defaultTaxRate;
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
                UnitCost = d.UnitCost,
                Discount = d.Discount,
                Subtotal = d.Quantity * d.UnitCost - d.Discount,
                BranchId = purchase.BranchId,
            }).ToList();

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
                    UnitCost = detail.UnitCost,
                    ReferenceNumber = purchase.PurchaseNumber,
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
            ?? throw new KeyNotFoundException("Purchase not found");

        if (purchase.Status == "cancelled")
            throw new InvalidOperationException("Purchase is already cancelled");

        purchase.Status = "cancelled";

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

        await _autoAccounting.ReversePurchaseEntryAsync(
            purchase.Id, purchase.Details.ToList(), purchase.Total);

        return await GetByIdAsync(purchase.Id) ?? throw new InvalidOperationException("Failed to cancel purchase");
    }

    public async Task CompleteAsync(Guid id)
    {
        var purchase = await _purchaseRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Purchase not found");

        purchase.Status = "completed";
        await _purchaseRepo.UpdateAsync(purchase);
        await _purchaseRepo.SaveChangesAsync();
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
            purchase.DueDate,
            purchase.InvoiceReference,
            purchase.Status,
            purchase.Subtotal,
            purchase.Tax,
            purchase.Discount,
            purchase.Total,
            purchase.PaidAmount,
            purchase.Balance,
            purchase.WithholdingType,
            purchase.WithholdingAmount,
            purchase.Notes,
            purchase.CurrencyCode,
            purchase.ExchangeRateToReporting,
            purchase.CountryCode,
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

        var items = await _purchaseRepo.GetFilteredAsync(filter.SupplierId, filter.Status, filter.FromDate, filter.ToDate, filter.Search, filter.BranchId ?? Guid.Empty, page, pageSize);
        var total = await _purchaseRepo.GetFilteredCountAsync(filter.SupplierId, filter.Status, filter.FromDate, filter.ToDate, filter.Search, filter.BranchId ?? Guid.Empty);

        return new PagedResult<PurchaseListResponse>(
            items.Select(p => new PurchaseListResponse(
                p.Id, p.PurchaseNumber, p.Supplier?.Name ?? "", p.CreatedAt, p.Status, p.Total, p.PaidAmount, p.Balance, p.CurrencyCode, p.CountryCode
            )).ToList(),
            total, page, pageSize
        );
    }

    public async Task<List<ApAgingResponse>> GetAgingAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Invalid tenant");

        var purchases = await _purchaseRepo.GetPendingAsync(Guid.Empty);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var grouped = purchases
            .GroupBy(p => new { p.SupplierId, SupplierName = p.Supplier?.Name ?? "" })
            .Select(g =>
            {
                var current = g.Where(p => !p.DueDate.HasValue || p.DueDate.Value >= today).Sum(p => p.Balance);
                var days30 = g.Where(p => p.DueDate.HasValue && p.DueDate.Value < today && p.DueDate.Value >= today.AddDays(-30)).Sum(p => p.Balance);
                var days60 = g.Where(p => p.DueDate.HasValue && p.DueDate.Value < today.AddDays(-30) && p.DueDate.Value >= today.AddDays(-60)).Sum(p => p.Balance);
                var days90 = g.Where(p => p.DueDate.HasValue && p.DueDate.Value < today.AddDays(-60) && p.DueDate.Value >= today.AddDays(-90)).Sum(p => p.Balance);
                var days90Plus = g.Where(p => p.DueDate.HasValue && p.DueDate.Value < today.AddDays(-90)).Sum(p => p.Balance);

                return new ApAgingResponse(
                    g.Key.SupplierId,
                    g.Key.SupplierName,
                    current,
                    days30,
                    days60,
                    days90,
                    days90Plus,
                    g.Sum(p => p.Balance)
                );
            })
            .OrderByDescending(a => a.TotalDue)
            .ToList();

        return grouped;
    }
}
