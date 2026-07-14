using AutoMapper;
using MassTransit;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Messages;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class SaleService
{
    private readonly ISaleRepository _saleRepo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IClientRepository _clientRepo;
    private readonly ICreditRepository _creditRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly IWebhookService _webhook;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly IGoalIntegrationService _goalIntegration;
    private readonly IAccountingPeriodRepository _periodRepo;
    private readonly IPublishEndpoint _publishEndpoint;

    public SaleService(
        ISaleRepository saleRepo,
        IProductRepository productRepo,
        IInventoryMovementRepository movementRepo,
        ICompanyRepository companyRepo,
        IClientRepository clientRepo,
        ICreditRepository creditRepo,
        IAutoAccountingService autoAccounting,
        IWebhookService webhook,
        ITenantContext tenant,
        IMapper mapper,
        IGoalIntegrationService goalIntegration,
        IAccountingPeriodRepository periodRepo,
        IPublishEndpoint publishEndpoint)
    {
        _saleRepo = saleRepo;
        _productRepo = productRepo;
        _movementRepo = movementRepo;
        _companyRepo = companyRepo;
        _clientRepo = clientRepo;
        _creditRepo = creditRepo;
        _autoAccounting = autoAccounting;
        _webhook = webhook;
        _tenant = tenant;
        _mapper = mapper;
        _goalIntegration = goalIntegration;
        _periodRepo = periodRepo;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<SaleResponse> CreateCashSaleAsync(CreateCashSaleRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");

        var openPeriod = await _periodRepo.GetCurrentOpenAsync(companyId);
        if (openPeriod is null)
            throw new InvalidOperationException(
                "No hay un período contable abierto. Debe abrir un período antes de registrar ventas.");

        await _saleRepo.BeginTransactionAsync();
        try
        {
            var settings = await _companyRepo.GetSettingsAsync(companyId);
            var defaultTaxRate = settings?.TaxRate ?? 0.15m;

            // 1. Cargar todos los productos involucrados con bloqueo de fila
            var productIds = request.Details.Select(d => d.ProductId).Distinct().ToList();
            var productMap = new Dictionary<Guid, Product>();
            foreach (var pid in productIds)
            {
                var prod = await _productRepo.GetByIdForUpdateAsync(pid)
                    ?? throw new InvalidOperationException($"Product not found: {pid}");
                productMap[pid] = prod;
            }

            decimal subtotal = 0;
            decimal totalTax = 0;

            // Calcular subtotales e impuestos
            foreach (var detail in request.Details)
            {
                var product = productMap[detail.ProductId];
                var lineSubtotal = detail.Quantity * detail.UnitPrice;
                subtotal += lineSubtotal;
                
                // Aplicar impuesto a nivel de producto o fallback al predeterminado
                var rate = product.TaxCategory?.Rate ?? defaultTaxRate; 
                totalTax += (lineSubtotal - detail.Discount) * rate;
            }

            var taxableAmount = subtotal - request.Discount;
            var total = taxableAmount + totalTax;

            var sale = _mapper.Map<Sale>(request);
            sale.CompanyId = companyId;
            sale.TenantId = _tenant.TenantId;
            sale.InvoiceNumber = await _saleRepo.GenerateInvoiceNumberAsync(companyId);
            sale.Subtotal = subtotal;
            sale.Tax = totalTax;
            sale.Total = total;
            sale.PaidAmount = total;
            sale.Balance = 0;

            var totalCost = 0m;
            foreach (var detail in request.Details)
            {
                var product = productMap[detail.ProductId];

                if (product.Stock < detail.Quantity)
                    throw new InvalidOperationException(
                        $"Stock insuficiente para: {product.Name}. Actual: {product.Stock}, Solicitado: {detail.Quantity}");

                totalCost += product.CostPrice * detail.Quantity;
            }

            var saleDetails = new List<SaleDetail>();
            foreach (var d in request.Details)
            {
                var product = productMap[d.ProductId];
                
                saleDetails.Add(new SaleDetail
                {
                    SaleId = sale.Id,
                    ProductId = d.ProductId,
                    Product = product,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Discount = d.Discount,
                    Subtotal = d.Quantity * d.UnitPrice - d.Discount,
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
                    BranchId = request.BranchId,
                }
            };

            await _saleRepo.AddAsync(sale);

            foreach (var detail in request.Details)
            {
                var product = productMap[detail.ProductId];
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
                    BranchId = request.BranchId,
                };
                await _movementRepo.AddAsync(movement);
            }

            await _saleRepo.SaveChangesAsync();

            await _autoAccounting.GenerateSaleEntryAsync(
                sale.Id, sale.Details.ToList(), request.Discount, request.Payment.Amount, "cash");
            await _autoAccounting.GenerateCostOfSaleEntryAsync(sale.Id, totalCost);

            // Registro de meta
            await _goalIntegration.HandleNewSaleAsync(request.EmployeeId, total);

            await _webhook.PublishAsync(sale.TenantId, "sale.created", new { SaleId = sale.Id, InvoiceNumber = sale.InvoiceNumber, Total = sale.Total, PaymentMethod = "cash" });

            await _saleRepo.CommitTransactionAsync();

            // Publish MassTransit event after commit
            await _publishEndpoint.Publish(new SaleCreatedEvent
            {
                SaleId = sale.Id,
                CompanyId = companyId,
                ClientId = request.ClientId,
                EmployeeId = request.EmployeeId,
                Total = sale.Total,
                Subtotal = sale.Subtotal,
                Tax = sale.Tax,
                SaleDate = DateTime.UtcNow,
                SaleType = "cash",
                Items = sale.Details.Select(d => new SaleItem
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? "",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal,
                }).ToList(),
            });

            return await GetByIdAsync(sale.Id) ?? throw new InvalidOperationException("Failed to create sale");
        }
        catch
        {
            await _saleRepo.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<SaleResponse> CreateCreditSaleAsync(CreateCreditSaleRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");

        var openPeriod = await _periodRepo.GetCurrentOpenAsync(companyId);
        if (openPeriod is null)
            throw new InvalidOperationException(
                "No hay un período contable abierto. Debe abrir un período antes de registrar ventas.");

        await _saleRepo.BeginTransactionAsync();
        try
        {
            var settings = await _companyRepo.GetSettingsAsync(companyId);
            var defaultTaxRate = settings?.TaxRate ?? 0.15m;

            // 1. Cargar todos los productos involucrados con bloqueo de fila
            var productIds = request.Details.Select(d => d.ProductId).Distinct().ToList();
            var productMap = new Dictionary<Guid, Product>();
            foreach (var pid in productIds)
            {
                var prod = await _productRepo.GetByIdForUpdateAsync(pid)
                    ?? throw new InvalidOperationException($"Product not found: {pid}");
                productMap[pid] = prod;
            }

            decimal subtotal = 0;
            decimal totalTax = 0;

            foreach (var detail in request.Details)
            {
                var product = productMap[detail.ProductId];
                var lineSubtotal = detail.Quantity * detail.UnitPrice;
                subtotal += lineSubtotal;
                var rate = product.TaxCategory?.Rate ?? defaultTaxRate;
                totalTax += (lineSubtotal - detail.Discount) * rate;
            }

            var total = (subtotal - request.Discount) + totalTax;

            var financedAmount = total - request.DownPayment;
            var interestAmount = financedAmount * (request.InterestRate / 100);
            var totalWithInterest = financedAmount + interestAmount;
            var installmentAmount = totalWithInterest / request.InstallmentCount;

            var client = await _clientRepo.GetByIdAsync(request.ClientId)
                ?? throw new InvalidOperationException("Client not found");

            if (client.CreditLimit.HasValue)
            {
                var activeCredits = await _creditRepo.GetFilteredAsync(request.ClientId, "active", null, request.BranchId, 1, int.MaxValue);
                activeCredits.AddRange(await _creditRepo.GetFilteredAsync(request.ClientId, "overdue", null, request.BranchId, 1, int.MaxValue));
                var currentExposure = activeCredits.Sum(c => c.Balance);
                if (currentExposure + financedAmount > client.CreditLimit.Value)
                    throw new InvalidOperationException(
                        $"El crÃ©dito excede el lÃ­mite del cliente. LÃ­mite: {client.CreditLimit.Value:N2}, " +
                        $"ExposiciÃ³n actual: {currentExposure:N2}, Nuevo financiamiento: {financedAmount:N2}");
            }

            var sale = _mapper.Map<Sale>(request);
            sale.CompanyId = companyId;
            sale.TenantId = _tenant.TenantId;
            sale.InvoiceNumber = await _saleRepo.GenerateInvoiceNumberAsync(companyId);
            sale.Subtotal = subtotal;
            sale.Tax = totalTax;
            sale.Total = total;
            sale.PaidAmount = request.DownPayment;
            sale.Balance = financedAmount;
            sale.Status = SaleStatus.Pending;

            var totalCost = 0m;
            foreach (var detail in request.Details)
            {
                var product = productMap[detail.ProductId];

                if (product.Stock < detail.Quantity)
                    throw new InvalidOperationException(
                        $"Stock insuficiente para: {product.Name}. Actual: {product.Stock}, Solicitado: {detail.Quantity}");

                totalCost += product.CostPrice * detail.Quantity;
            }

            var saleDetails = new List<SaleDetail>();
            foreach (var d in request.Details)
            {
                var product = productMap[d.ProductId];
                
                saleDetails.Add(new SaleDetail
                {
                    SaleId = sale.Id,
                    ProductId = d.ProductId,
                    Product = product,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Discount = d.Discount,
                    Subtotal = d.Quantity * d.UnitPrice - d.Discount,
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
                        BranchId = request.BranchId,
                    }
                };
            }

            var credit = new Credit
            {
                CreditNumber = await _creditRepo.GenerateCreditNumberAsync(companyId),
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
                    BranchId = request.BranchId,
                });
            }

            sale.Credit = credit;

            await _saleRepo.AddAsync(sale);

            foreach (var detail in request.Details)
            {
                var product = productMap[detail.ProductId];
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
                    BranchId = request.BranchId,
                };
                await _movementRepo.AddAsync(movement);
            }

            await _saleRepo.SaveChangesAsync();

            await _autoAccounting.GenerateSaleEntryAsync(
                sale.Id, sale.Details.ToList(), request.Discount, request.DownPayment, "credit");
            await _autoAccounting.GenerateCostOfSaleEntryAsync(sale.Id, totalCost);

            // Registro de meta
            await _goalIntegration.HandleNewSaleAsync(request.EmployeeId, total);

            await _webhook.PublishAsync(sale.TenantId, "sale.created", new { SaleId = sale.Id, InvoiceNumber = sale.InvoiceNumber, Total = sale.Total, PaymentMethod = "credit" });

            await _saleRepo.CommitTransactionAsync();

            // Publish MassTransit event after commit
            await _publishEndpoint.Publish(new SaleCreatedEvent
            {
                SaleId = sale.Id,
                CompanyId = companyId,
                ClientId = request.ClientId,
                EmployeeId = request.EmployeeId,
                Total = sale.Total,
                Subtotal = sale.Subtotal,
                Tax = sale.Tax,
                SaleDate = DateTime.UtcNow,
                SaleType = "credit",
                Items = sale.Details.Select(d => new SaleItem
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? "",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal,
                }).ToList(),
            });

            return await GetByIdAsync(sale.Id) ?? throw new InvalidOperationException("Failed to create credit sale");
        }
        catch
        {
            await _saleRepo.RollbackTransactionAsync();
            throw;
        }
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

        var items = await _saleRepo.GetFilteredAsync(filter.ClientId, filter.SaleType, filter.Status, filter.FromDate, filter.ToDate, filter.Search, Guid.Empty, page, pageSize);
        var total = await _saleRepo.GetFilteredCountAsync(filter.ClientId, filter.SaleType, filter.Status, filter.FromDate, filter.ToDate, filter.Search, Guid.Empty);

        return new PagedResult<SaleListResponse>(
            _mapper.Map<List<SaleListResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task CancelSaleAsync(Guid id)
    {
        var sale = await _saleRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Sale not found");

        if (sale.Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("Sale is already cancelled");

        if (sale.Balance > 0)
            throw new InvalidOperationException("Cannot cancel a sale with outstanding balance. Process a credit note first.");

        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");

        var openPeriod = await _periodRepo.GetCurrentOpenAsync(companyId);
        if (openPeriod is null)
            throw new InvalidOperationException(
                "No hay un período contable abierto. Debe abrir un período antes de cancelar ventas.");

        await _saleRepo.BeginTransactionAsync();
        try
        {
            sale.Status = SaleStatus.Cancelled;
            await _saleRepo.SaveChangesAsync();

            // Reverse sale accounting entries
            await _autoAccounting.ReverseSaleEntryAsync(
                sale.Id, sale.Details.ToList(), sale.Discount, sale.SaleType);
            await _autoAccounting.GenerateCostOfSaleEntryAsync(
                sale.Id, -(sale.Details.Sum(d => d.Quantity * (d.Product?.CostPrice ?? 0))));

            await _saleRepo.CommitTransactionAsync();

            // Publish MassTransit event after commit
            await _publishEndpoint.Publish(new SaleCancelledEvent
            {
                SaleId = sale.Id,
                CompanyId = companyId,
                Reason = "Sale cancelled by user",
                CancelledAt = DateTime.UtcNow,
                ReturnedItems = sale.Details.Select(d => new SaleItem
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? "",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal,
                }).ToList(),
            });
        }
        catch
        {
            await _saleRepo.RollbackTransactionAsync();
            throw;
        }
    }
}
