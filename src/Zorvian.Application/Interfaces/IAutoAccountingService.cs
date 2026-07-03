using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IAutoAccountingService
{
    // New requirement
    Task GenerateEntryAsync(string processTrigger, Guid companyId, string countryCode, object context);
    
    // Original methods reconstructed from code analysis
    Task<Guid> GenerateSaleEntryAsync(Guid saleId, List<SaleDetail> details, decimal discount, decimal paidAmount, string saleType, Guid? costCenterId = null);
    Task<Guid> GenerateCostOfSaleEntryAsync(Guid saleId, decimal totalCost, Guid? costCenterId = null);
    Task<Guid> GeneratePurchaseEntryAsync(Guid purchaseId, List<PurchaseDetail> details, decimal discount, decimal total, string countryCode);
    Task<Guid> GenerateCashMovementEntryAsync(Guid cashMovementId);
    Task<Guid> GenerateCreditPaymentEntryAsync(Guid paymentId, Guid creditId, decimal principal, decimal interest, Guid companyId, Guid branchId);
    Task<Guid> GenerateSupplierPaymentEntryAsync(Guid paymentId, Guid purchaseId, decimal amount, Guid companyId, Guid branchId);
    Task<Guid> GenerateSupplierCreditNoteEntryAsync(Guid creditNoteId, Guid supplierId, Guid? purchaseId, decimal total, Guid companyId, Guid branchId);
    Task<Guid> GenerateCreditNoteEntryAsync(Guid creditNoteId, Guid saleId, string saleType, List<CreditNoteDetail> details, decimal subtotal, decimal tax);
    Task<Guid> GenerateWarrantyCostEntryAsync(Guid costId, string category, decimal totalCost, string paidBy, Guid? paidByPartyId, Guid warrantyId, Guid companyId, Guid branchId);
    Task<Guid> GenerateInventoryEntryAsync(Guid movementId, Guid productId, string movementType, decimal quantity, decimal unitCost);
    Task<Guid> GenerateFixedAssetAcquisitionEntryAsync(Guid assetId, decimal cost, Guid companyId, Guid branchId);
    Task<Guid> GenerateDepreciationEntryAsync(Guid entryId, decimal amount, Guid companyId, Guid branchId);
    Task<Guid> GenerateRevaluationEntryAsync(Guid revaluationId, decimal previousValue, decimal newValue, decimal accumulatedDep, Guid companyId, Guid branchId);
    Task<Guid> GenerateDisposalEntryAsync(Guid disposalId, decimal acquisitionCost, decimal accumulatedDep, decimal saleAmount, decimal gainOrLoss, string disposalType, Guid companyId, Guid branchId);
    Task<Guid> GeneratePayrollEntryAsync(Guid payrollRunId);
    Task ReversePurchaseEntryAsync(Guid purchaseId, List<PurchaseDetail> details, decimal total);
    Task ReverseSaleEntryAsync(Guid saleId, List<SaleDetail> details, decimal discount, string saleType, Guid? costCenterId = null);

    // Treasury methods - restored to accept Nullable Guids as expected by Controllers
    Task<Guid> GenerateCheckEntryAsync(Guid checkId, decimal amount, string checkType, Guid? bankAccountId, Guid? payeeId, Guid? costCenterId);
    Task<Guid> GenerateBankDepositEntryAsync(Guid depositId, decimal amount, Guid bankAccountId, Guid? costCenterId);
    Task<Guid> GenerateBankTransferEntryAsync(Guid transferId, decimal amount, Guid fromAccountId, Guid toAccountId, Guid? costCenterId);
    Task<Guid> GenerateBankCommissionEntryAsync(Guid commissionId, decimal commission, Guid bankAccountId, Guid? costCenterId);
    Task<Guid> GenerateCollectionEntryAsync(Guid collectionId, decimal amount, decimal interest, decimal lateFee, Guid invoiceId, Guid? costCenterId);
    Task<Guid> GenerateAdvanceToSupplierEntryAsync(Guid advanceId, decimal amount, Guid supplierId, Guid? costCenterId);
    Task<Guid> GenerateSupplierAdvanceApplicationEntryAsync(Guid applicationId, decimal amount, Guid advanceId, Guid purchaseId, Guid? costCenterId);
    Task<Guid> GenerateFleetExpenseEntryAsync(Guid expenseId, decimal amount, string description, Guid? accountId, Guid companyId, Guid? branchId);
}
