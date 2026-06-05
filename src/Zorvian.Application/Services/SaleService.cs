using AutoMapper;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class SaleService
{
    private readonly ISaleRepository _saleRepo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public SaleService(
        ISaleRepository saleRepo,
        IProductRepository productRepo,
        IInventoryMovementRepository movementRepo,
        ICompanyRepository companyRepo,
        IAutoAccountingService autoAccounting,
        ITenantContext tenant,
        IMapper mapper)
    {
        _saleRepo = saleRepo;
        _productRepo = productRepo;
        _movementRepo = movementRepo;
        _companyRepo = companyRepo;
        _autoAccounting = autoAccounting;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<SaleResponse> CreateCashSaleAsync(CreateCashSaleRequest request)
    {
        var companyId = Guid.Parse(_tenant.TenantId);

        decimal subtotal = 0;
        decimal totalTax = 0;

        // Calculate subtotal and tax per product
        foreach (var detail in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(detail.ProductId)
                ?? throw new InvalidOperationException($"Product not found: {detail.ProductId}");

            var lineSubtotal = detail.Quantity * detail.UnitPrice;
            subtotal += lineSubtotal;
            
            // Apply product-level tax
            var rate = product.TaxCategory?.Rate ?? 0m; 
            totalTax += (lineSubtotal - detail.Discount) * rate;
        }

        var taxableAmount = subtotal - request.Discount;
        var total = taxableAmount + totalTax;

        var sale = _mapper.Map<Sale>(request);
        sale.InvoiceNumber = await _saleRepo.GenerateInvoiceNumberAsync(companyId);
        sale.Subtotal = subtotal;
        sale.Tax = totalTax;
        sale.Total = total;
        sale.PaidAmount = total;
        sale.Balance = 0;
        sale.CompanyId = companyId;

        // Refactored to include Product navigation property
        var saleDetails = new List<SaleDetail>();
        foreach (var d in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(d.ProductId)
                ?? throw new InvalidOperationException($"Product not found: {d.ProductId}");
            
            saleDetails.Add(new SaleDetail
            {
                SaleId = sale.Id,
                ProductId = d.ProductId,
                Product = product,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Discount = d.Discount,
                Subtotal = d.Quantity * d.UnitPrice - d.Discount,
                CompanyId = companyId,
                BranchId = request.BranchId,
            });
        }
        sale.Details = saleDetails;

        sale.Payments = new List<SalePayment>
        {
            new()
            {
                SaleId = sale.Id,
                Amount = request.Payment.Amount,
                PaymentMethod = request.Payment.PaymentMethod,
                ReferenceNumber = request.Payment.ReferenceNumber,
                PaymentDate = DateTime.UtcNow,
                CashRegisterId = request.Payment.CashRegisterId,
                CompanyId = companyId,
                BranchId = request.BranchId,
            }
        };

        await _saleRepo.AddAsync(sale);

        decimal totalCost = 0;
        foreach (var detail in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(detail.ProductId);
            if (product is null) continue;

            totalCost += product.CostPrice * detail.Quantity;

            var stockBefore = product.Stock;
            product.Stock -= detail.Quantity;
            var movement = new InventoryMovement
            {
                ProductId = detail.ProductId,
                MovementType = "sale",
                Quantity = detail.Quantity,
                StockBefore = stockBefore,
                StockAfter = product.Stock,
                UnitCost = product.CostPrice,
                ReferenceNumber = sale.InvoiceNumber,
                PerformedByEmployeeId = request.EmployeeId,
                CompanyId = companyId,
                BranchId = request.BranchId,
            };
            await _movementRepo.AddAsync(movement);
        }

        await _saleRepo.SaveChangesAsync();

        await _autoAccounting.GenerateSaleEntryAsync(
            sale.Id, sale.Details.ToList(), request.Discount, request.Payment.Amount, "cash");
        await _autoAccounting.GenerateCostOfSaleEntryAsync(sale.Id, totalCost);

        return await GetByIdAsync(sale.Id) ?? throw new InvalidOperationException("Failed to create sale");
    }

    public async Task<SaleResponse> CreateCreditSaleAsync(CreateCreditSaleRequest request)
    {
        var companyId = Guid.Parse(_tenant.TenantId);

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

        var financedAmount = total - request.DownPayment;
        var interestAmount = financedAmount * (request.InterestRate / 100);
        var totalWithInterest = financedAmount + interestAmount;
        var installmentAmount = totalWithInterest / request.InstallmentCount;

        var sale = _mapper.Map<Sale>(request);
        sale.InvoiceNumber = await _saleRepo.GenerateInvoiceNumberAsync(companyId);
        sale.Subtotal = subtotal;
        sale.Tax = totalTax;
        sale.Total = total;
        sale.PaidAmount = request.DownPayment;
        sale.Balance = financedAmount;
        sale.Status = "pending";
        sale.CompanyId = companyId;

        // Refactored to include Product navigation property
        var saleDetails = new List<SaleDetail>();
        foreach (var d in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(d.ProductId)
                ?? throw new InvalidOperationException($"Product not found: {d.ProductId}");
            
            saleDetails.Add(new SaleDetail
            {
                SaleId = sale.Id,
                ProductId = d.ProductId,
                Product = product,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Discount = d.Discount,
                Subtotal = d.Quantity * d.UnitPrice - d.Discount,
                CompanyId = companyId,
                BranchId = request.BranchId,
            });
        }
        sale.Details = saleDetails;

        if (request.DownPayment > 0)
        {
            sale.Payments = new List<SalePayment>
            {
                new()
                {
                    SaleId = sale.Id,
                    Amount = request.DownPayment,
                    PaymentMethod = "cash",
                    PaymentDate = DateTime.UtcNow,
                    CompanyId = companyId,
                    BranchId = request.BranchId,
                }
            };
        }

        var credit = new Credit
        {
            CreditNumber = $"CRE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            ClientId = request.ClientId,
            SaleId = sale.Id,
            EmployeeId = request.EmployeeId,
            FinancedAmount = financedAmount,
            InterestRate = request.InterestRate,
            InstallmentCount = request.InstallmentCount,
            InstallmentAmount = installmentAmount,
            TotalAmount = totalWithInterest,
            PaidAmount = request.DownPayment,
            Balance = totalWithInterest,
            InterestAmount = interestAmount,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(request.InstallmentCount)),
            NextDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = "active",
            CompanyId = companyId,
            BranchId = request.BranchId,
        };

        for (int i = 1; i <= request.InstallmentCount; i++)
        {
            credit.Installments.Add(new CreditInstallment
            {
                CreditId = credit.Id,
                InstallmentNumber = i,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(i)),
                Amount = installmentAmount,
                PrincipalAmount = financedAmount / request.InstallmentCount,
                InterestAmount = interestAmount / request.InstallmentCount,
                PaidAmount = 0,
                Balance = installmentAmount,
                Status = "pending",
                CompanyId = companyId,
                BranchId = request.BranchId,
            });
        }

        sale.Credit = credit;

        await _saleRepo.AddAsync(sale);

        decimal totalCost = 0;
        foreach (var detail in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(detail.ProductId);
            if (product is null) continue;

            totalCost += product.CostPrice * detail.Quantity;

            var stockBefore = product.Stock;
            product.Stock -= detail.Quantity;
            var movement = new InventoryMovement
            {
                ProductId = detail.ProductId,
                MovementType = "sale",
                Quantity = detail.Quantity,
                StockBefore = stockBefore,
                StockAfter = product.Stock,
                UnitCost = product.CostPrice,
                ReferenceNumber = sale.InvoiceNumber,
                PerformedByEmployeeId = request.EmployeeId,
                CompanyId = companyId,
                BranchId = request.BranchId,
            };
            await _movementRepo.AddAsync(movement);
        }

        await _saleRepo.SaveChangesAsync();

        await _autoAccounting.GenerateSaleEntryAsync(
            sale.Id, sale.Details.ToList(), request.Discount, request.DownPayment, "credit");
        await _autoAccounting.GenerateCostOfSaleEntryAsync(sale.Id, totalCost);

        return await GetByIdAsync(sale.Id) ?? throw new InvalidOperationException("Failed to create credit sale");
    }

    public async Task<SaleResponse?> GetByIdAsync(Guid id)
    {
        var sale = await _saleRepo.GetByIdAsync(id);
        return sale is null ? null : _mapper.Map<SaleResponse>(sale);
    }

    public async Task<PagedResult<SaleListResponse>> GetFilteredAsync(SaleFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _saleRepo.GetFilteredAsync(filter.ClientId, filter.SaleType, filter.Status, filter.FromDate, filter.ToDate, Guid.Empty, page, pageSize);
        var total = await _saleRepo.GetFilteredCountAsync(filter.ClientId, filter.SaleType, filter.Status, filter.FromDate, filter.ToDate, Guid.Empty);

        return new PagedResult<SaleListResponse>(
            _mapper.Map<List<SaleListResponse>>(items),
            total, page, pageSize
        );
    }
}
