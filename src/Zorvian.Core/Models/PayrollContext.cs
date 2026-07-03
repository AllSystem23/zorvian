using Zorvian.Core.Entities;

namespace Zorvian.Core.Models;

public record PayrollContext(
    int EmployeeCount,
    bool IsLivingIn, // Específico para Nicaragua, pero útil en otros contextos
    TerminationContext? Termination = null
);

public record TerminationContext(
    TerminationReason Type,
    DateTime HireDate,
    DateTime TerminationDate,
    decimal MonthlySalary
);
