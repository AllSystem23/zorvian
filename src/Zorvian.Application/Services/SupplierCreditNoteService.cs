using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class SupplierCreditNoteService
{
    private readonly ISupplierCreditNoteRepository _creditNoteRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly IProductRepository _productRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ITenantContext _tenant;

    public SupplierCreditNoteService(
        ISupplierCreditNoteRepository creditNoteRepo,
        IPurchaseRepository purchaseRepo,
        IInventoryMovementRepository movementRepo,
        IProductRepository productRepo,
        IAutoAccountingService autoAccounting,
        ITenantContext tenant)
    {
        _creditNoteRepo = creditNoteRepo;
        _purchaseRepo = purchaseRepo;
        _movementRepo = movementRepo;
        _productRepo = productRepo;
        _autoAccounting = autoAccounting;
        _tenant = tenant;
    }

    public async Task<SupplierCreditNoteResponse> CreateAsync(CreateSupplierCreditNoteRequest request)
    {
        var companyId = _tenant.RequireCompanyId();

        var creditNote = new SupplierCreditNote
        {
            CreditNoteNumber = await _creditNoteRepo.GenerateCreditNoteNumberAsync(companyId),
            SupplierId = request.SupplierId,
            PurchaseId = request.PurchaseId,
            CreditNoteDate = request.CreditNoteDate,
            Reason = request.Reason ?? string.Empty,
            Subtotal = request.Subtotal,
            Tax = request.Tax,
            Total = request.Total,
            Status = "completed",
            BranchId = request.BranchId,
        };

        await _creditNoteRepo.AddAsync(creditNote);

        if (request.PurchaseId.HasValue)
        {
            var purchase = await _purchaseRepo.GetByIdAsync(request.PurchaseId.Value);
            if (purchase is not null)
            {
                purchase.Balance = Math.Max(0, purchase.Balance - request.Total);
                if (purchase.Balance <= 0 && purchase.PaidAmount >= purchase.Total)
                    purchase.Status = "completed";
                await _purchaseRepo.UpdateAsync(purchase);
            }
        }

        if (request.Details is not null)
        {
            foreach (var detail in request.Details)
            {
                var product = await _productRepo.GetByIdAsync(detail.ProductId);
                if (product is null) continue;

                var stockBefore = product.Stock;
                product.Stock -= detail.Quantity;

                var movement = new InventoryMovement
                {
                    ProductId = detail.ProductId,
                    MovementType = "purchase_return",
                    Quantity = detail.Quantity,
                    StockBefore = stockBefore,
                    StockAfter = product.Stock,
                    UnitCost = detail.UnitPrice,
                    ReferenceNumber = creditNote.CreditNoteNumber,
                    BranchId = request.BranchId,
                };
                await _movementRepo.AddAsync(movement);
            }
        }

        await _creditNoteRepo.SaveChangesAsync();

        await _autoAccounting.GenerateSupplierCreditNoteEntryAsync(
            creditNote.Id, request.SupplierId, request.PurchaseId, request.Total, companyId, request.BranchId);

        return await GetByIdAsync(creditNote.Id) ?? throw new InvalidOperationException("Failed to create credit note");
    }

    public async Task<SupplierCreditNoteResponse?> GetByIdAsync(Guid id)
    {
        var cn = await _creditNoteRepo.GetByIdAsync(id);
        if (cn is null) return null;

        return new SupplierCreditNoteResponse(
            cn.Id, cn.CreditNoteNumber, cn.SupplierId, cn.Supplier?.Name ?? "",
            cn.PurchaseId, cn.CreditNoteDate, cn.Reason,
            cn.Subtotal, cn.Tax, cn.Total, cn.Status
        );
    }

    public async Task<List<SupplierCreditNoteResponse>> GetByPurchaseIdAsync(Guid purchaseId)
    {
        var notes = await _creditNoteRepo.GetByPurchaseIdAsync(purchaseId);
        return notes.Select(cn => new SupplierCreditNoteResponse(
            cn.Id, cn.CreditNoteNumber, cn.SupplierId, cn.Supplier?.Name ?? "",
            cn.PurchaseId, cn.CreditNoteDate, cn.Reason,
            cn.Subtotal, cn.Tax, cn.Total, cn.Status
        )).ToList();
    }

    public async Task<List<SupplierCreditNoteResponse>> GetAllAsync()
    {
        var companyId = _tenant.RequireCompanyId();
        var notes = await _creditNoteRepo.GetAllAsync(companyId);
        return notes.Select(cn => new SupplierCreditNoteResponse(
            cn.Id, cn.CreditNoteNumber, cn.SupplierId, cn.Supplier?.Name ?? "",
            cn.PurchaseId, cn.CreditNoteDate, cn.Reason,
            cn.Subtotal, cn.Tax, cn.Total, cn.Status
        )).ToList();
    }
}
