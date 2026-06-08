using AutoMapper;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class CreditNoteService
{
    private readonly ICreditNoteRepository _repo;
    private readonly ISaleRepository _saleRepo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public CreditNoteService(
        ICreditNoteRepository repo,
        ISaleRepository saleRepo,
        IProductRepository productRepo,
        IInventoryMovementRepository movementRepo,
        IAutoAccountingService autoAccounting,
        ITenantContext tenant,
        IMapper mapper)
    {
        _repo = repo; _saleRepo = saleRepo;
        _productRepo = productRepo; _movementRepo = movementRepo;
        _autoAccounting = autoAccounting; _tenant = tenant; _mapper = mapper;
    }

    private Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    public async Task<List<CreditNoteResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync(CompanyId);
        return _mapper.Map<List<CreditNoteResponse>>(items);
    }

    public async Task<CreditNoteResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<CreditNoteResponse>(item);
    }

    public async Task<List<CreditNoteResponse>> GetBySaleIdAsync(Guid saleId)
    {
        var items = await _repo.GetBySaleIdAsync(saleId);
        return _mapper.Map<List<CreditNoteResponse>>(items);
    }

    public async Task<CreditNoteResponse> CreateAsync(CreateCreditNoteRequest request)
    {
        var sale = await _saleRepo.GetByIdAsync(request.SaleId)
            ?? throw new InvalidOperationException("Sale not found");
        var companyId = CompanyId;
        var creditNoteNumber = await _repo.GenerateCreditNoteNumberAsync(companyId);

        var details = new List<CreditNoteDetail>();
        decimal totalSubtotal = 0, totalTax = 0, total = 0;

        foreach (var item in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId)
                ?? throw new InvalidOperationException($"Product {item.ProductId} not found");

            var rate = product.TaxCategory?.Rate ?? 0;
            var lineSubtotal = item.Quantity * item.UnitPrice;
            var lineTax = lineSubtotal * rate;
            var lineTotal = lineSubtotal + lineTax;

            details.Add(new CreditNoteDetail
            {
                ProductId = item.ProductId, Quantity = item.Quantity,
                UnitPrice = item.UnitPrice, Subtotal = lineSubtotal,
                Tax = lineTax, Total = lineTotal,
                CompanyId = companyId, BranchId = sale.BranchId,
            });

            totalSubtotal += lineSubtotal;
            totalTax += lineTax;
            total += lineTotal;

            var stockBefore = product.Stock;
            product.Stock += item.Quantity;

            await _movementRepo.AddAsync(new InventoryMovement
            {
                ProductId = item.ProductId, MovementType = "credit_note",
                Quantity = item.Quantity, StockBefore = stockBefore,
                StockAfter = product.Stock, UnitCost = product.CostPrice,
                ReferenceNumber = creditNoteNumber, CompanyId = companyId,
                BranchId = sale.BranchId,
            });
        }

        var creditNote = new CreditNote
        {
            CreditNoteNumber = creditNoteNumber,
            SaleId = request.SaleId,
            IssueDate = DateTime.UtcNow,
            Status = "issued",
            Reason = request.Reason,
            Subtotal = totalSubtotal, Tax = totalTax, Total = total,
            CompanyId = companyId, BranchId = sale.BranchId,
            Details = details,
        };

        await _repo.AddAsync(creditNote);
        await _repo.SaveChangesAsync();

        await _autoAccounting.GenerateCreditNoteEntryAsync(creditNote.Id, request.SaleId, sale.SaleType, details, totalSubtotal, totalTax);

        return (await GetByIdAsync(creditNote.Id))!;
    }
}
