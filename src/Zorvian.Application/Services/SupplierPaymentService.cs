using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class SupplierPaymentService
{
    private readonly ISupplierPaymentRepository _paymentRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ITenantContext _tenant;

    public SupplierPaymentService(
        ISupplierPaymentRepository paymentRepo,
        IPurchaseRepository purchaseRepo,
        IAutoAccountingService autoAccounting,
        ITenantContext tenant)
    {
        _paymentRepo = paymentRepo;
        _purchaseRepo = purchaseRepo;
        _autoAccounting = autoAccounting;
        _tenant = tenant;
    }

    public async Task<SupplierPaymentResponse> RegisterPaymentAsync(CreateSupplierPaymentRequest request)
    {
        var purchase = await _purchaseRepo.GetByIdAsync(request.PurchaseId)
            ?? throw new KeyNotFoundException("Purchase not found");

        if (purchase.Status == "cancelled")
            throw new InvalidOperationException("Cannot pay a cancelled purchase");

        if (request.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be positive");

        var payment = new SupplierPayment
        {
            PurchaseId = request.PurchaseId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber,
            PaymentDate = DateTime.UtcNow,
            Notes = request.Notes,
            CompanyId = purchase.CompanyId,
            BranchId = purchase.BranchId,
        };

        purchase.PaidAmount += request.Amount;
        purchase.Balance = Math.Max(0, purchase.Total - purchase.PaidAmount);

        if (purchase.Balance <= 0)
            purchase.Status = "completed";

        await _paymentRepo.AddAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        await _autoAccounting.GenerateSupplierPaymentEntryAsync(
            payment.Id, purchase.Id, request.Amount, purchase.CompanyId, purchase.BranchId);

        return new SupplierPaymentResponse(
            payment.Id,
            payment.PurchaseId,
            purchase.PurchaseNumber,
            payment.Amount,
            payment.PaymentMethod,
            payment.ReferenceNumber,
            payment.PaymentDate,
            payment.Notes
        );
    }

    public async Task<List<SupplierPaymentResponse>> GetByPurchaseIdAsync(Guid purchaseId)
    {
        var payments = await _paymentRepo.GetByPurchaseIdAsync(purchaseId);
        return payments.Select(p => new SupplierPaymentResponse(
            p.Id, p.PurchaseId, p.Purchase?.PurchaseNumber ?? "", p.Amount,
            p.PaymentMethod, p.ReferenceNumber, p.PaymentDate, p.Notes
        )).ToList();
    }

    public async Task<List<SupplierPaymentResponse>> GetAllAsync()
    {
        var companyId = _tenant.RequireCompanyId();
        var payments = await _paymentRepo.GetAllAsync(companyId);
        return payments.Select(p => new SupplierPaymentResponse(
            p.Id, p.PurchaseId, p.Purchase?.PurchaseNumber ?? "", p.Amount,
            p.PaymentMethod, p.ReferenceNumber, p.PaymentDate, p.Notes
        )).ToList();
    }
}
