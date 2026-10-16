using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

/// <summary>
/// Períodos de contrato de un trabajador (historial laboral: liquidar →
/// recontratar sin perder el rastro). El período abierto es el contrato vigente.
/// </summary>
public interface IEmploymentPeriodRepository
{
    Task<EmploymentPeriod?> GetByIdAsync(Guid id);

    /// <summary>Períodos del empleado, del más reciente al más antiguo.</summary>
    Task<List<EmploymentPeriod>> GetByEmployeeAsync(Guid employeeId);

    /// <summary>Período abierto (contrato vigente) del empleado, si existe.</summary>
    Task<EmploymentPeriod?> GetOpenByEmployeeAsync(Guid employeeId);

    /// <summary>Siguiente número de período (1 = primera contratación).</summary>
    Task<int> GetNextPeriodNumberAsync(Guid employeeId);

    Task AddAsync(EmploymentPeriod period);

    Task UpdateAsync(EmploymentPeriod period);

    Task SaveChangesAsync();
}
