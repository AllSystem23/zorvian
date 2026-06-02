namespace Zorvian.Application.DTOs.Commercial;

public sealed record SaleDetailItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Subtotal
);

public sealed record CreateSaleRequest(
    Guid ClientId,
    Guid EmployeeId,
    string SaleType,
    decimal Discount,
    string? Notes,
    Guid BranchId,
    List<SaleDetailItem> Details
);

public sealed record SalePaymentInfo(
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    Guid? CashRegisterId
);

public sealed record CreateCashSaleRequest(
    Guid ClientId,
    Guid EmployeeId,
    decimal Discount,
    string? Notes,
    Guid BranchId,
    List<SaleDetailItem> Details,
    SalePaymentInfo Payment
);

public sealed record CreateCreditSaleRequest(
    Guid ClientId,
    Guid EmployeeId,
    decimal Discount,
    string? Notes,
    Guid BranchId,
    List<SaleDetailItem> Details,
    decimal DownPayment,
    int InstallmentCount,
    decimal InterestRate
);

public sealed record SaleResponse(
    Guid Id,
    string InvoiceNumber,
    Guid ClientId,
    string ClientName,
    Guid EmployeeId,
    string EmployeeName,
    DateTime SaleDate,
    string SaleType,
    decimal Subtotal,
    decimal Tax,
    decimal Discount,
    decimal Total,
    decimal PaidAmount,
    decimal Balance,
    string Status,
    string? Notes,
    List<SaleDetailItem> Details,
    Guid? CreditId
);

public sealed record SaleListResponse(
    Guid Id,
    string InvoiceNumber,
    string ClientName,
    DateTime SaleDate,
    string SaleType,
    decimal Total,
    decimal Balance,
    string Status
);

public sealed record SaleFilterRequest(
    Guid? ClientId,
    string? SaleType,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int? Page = 1,
    int? PageSize = 20
);
