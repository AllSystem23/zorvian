using Zorvian.Core.Entities;

namespace Zorvian.Application.DTOs.Payroll;

public record CalculateTerminationRequest(
    Guid EmployeeId,
    DateTime TerminationDate,
    string TerminationReason
);

public record TerminationCalculationResponse(
    decimal TotalIndemnity,
    decimal TotalAguinaldo,
    decimal TotalVacation,
    decimal GrandTotal
);
