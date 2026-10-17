namespace Zorvian.Application.DTOs.ServiceProviderPayment;

public sealed record CreateServiceProviderPaymentRequest(
    Guid EmployeeId,
    int PeriodYear,
    int PeriodMonth,
    int PeriodHalf,
    DateOnly PaymentDate,
    string? GoalEvaluationMode,   // "total" (default) | "individual"
    decimal GoalBonusAmount,      // bono fijo por meta (ej. 1,000.00)
    decimal OtherIncome,
    string? Notes,
    List<ServiceProviderPaymentLineDto> Lines,
    List<ServiceProviderPaymentDeductionDto> Deductions
);

/// <summary>Línea de recaudo por nombre (OTROS, TELÉFONO o custom). El monto de comisión
/// lo calcula el servidor = CollectedAmount × CommissionRate / 100.</summary>
public sealed record ServiceProviderPaymentLineDto(
    string LineName,
    decimal CollectedAmount,   // recaudo en efectivo (editable)
    decimal ContractedAmount,  // meta/contrato por línea (editable)
    decimal CommissionRate     // % p.ej. 4
);

/// <summary>Deducción de un pago (adelanto, crédito de tienda, préstamo, INSS u otra).
/// AgreementReference/AgreementDate son OPCIONALES: no se exigen para ningún tipo;
/// si se registran se muestran en el comprobante.</summary>
public sealed record ServiceProviderPaymentDeductionDto(
    string DeductionType,
    string? Description,
    decimal Amount,
    string? AgreementReference,
    DateOnly? AgreementDate
);

public sealed record UpdateServiceProviderPaymentRequest(
    DateOnly PaymentDate,
    string? GoalEvaluationMode,
    decimal GoalBonusAmount,
    decimal OtherIncome,
    string? Notes,
    List<ServiceProviderPaymentLineDto> Lines,
    List<ServiceProviderPaymentDeductionDto> Deductions
);

public sealed record MarkServiceProviderPaymentPaidRequest(string? PaymentReference);

public sealed record ServiceProviderPaymentResponse(
    Guid Id,
    string PaymentNumber,
    Guid EmployeeId,
    string? EmployeeName,
    string? EmployeeCode,
    Guid? BranchId,            // sucursal DEL PRESTADOR (heredada de Employee.BranchId)
    string? BranchName,
    int PeriodYear,
    int PeriodMonth,
    int PeriodHalf,
    string PeriodLabel,          // "Quincena 1 · Mar 2026"
    DateOnly PaymentDate,
    string Status,
    string GoalEvaluationMode,   // total | individual
    bool GoalMet,
    decimal GoalBonusAmount,     // bono configurado (solo aplica si GoalMet)
    decimal OtherIncome,
    decimal TotalCollected,
    decimal TotalContracted,
    decimal TotalCommission,
    decimal TotalAccumulated,
    decimal TotalDeductions,
    decimal NetAmount,
    string? Notes,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string? PaymentReference,
    List<ServiceProviderPaymentLineResponse> Lines,
    List<ServiceProviderPaymentDeductionResponse> Deductions
);

public sealed record ServiceProviderPaymentLineResponse(
    Guid Id,
    string LineName,
    decimal CollectedAmount,
    decimal ContractedAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    bool LineGoalMet
);

public sealed record ServiceProviderPaymentDeductionResponse(
    Guid Id,
    string DeductionType,
    string? Description,
    decimal Amount,
    string? AgreementReference,
    DateOnly? AgreementDate
);
