using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IVacationPaymentService
{
    /// <summary>
    /// Processes a vacation payment for an approved vacation request.
    /// Marks the request as "taken", updates LeaveBalances, and creates a payment receipt.
    /// </summary>
    Task<VacationPaymentReceipt> PayVacationAsync(Guid vacationRequestId, DateOnly? paymentDate = null, string? observations = null, string? paidBy = null);

    /// <summary>
    /// Gets a payment receipt by its ID.
    /// </summary>
    Task<VacationPaymentReceipt?> GetReceiptByIdAsync(Guid id);

    /// <summary>
    /// Gets the payment receipt for a specific vacation request (if already paid).
    /// </summary>
    Task<VacationPaymentReceipt?> GetReceiptByRequestIdAsync(Guid vacationRequestId);

    /// <summary>
    /// Lists payment receipts with optional filters.
    /// </summary>
    Task<List<VacationPaymentReceipt>> GetReceiptsAsync(Guid? employeeId, int? year, int page, int pageSize);
}
