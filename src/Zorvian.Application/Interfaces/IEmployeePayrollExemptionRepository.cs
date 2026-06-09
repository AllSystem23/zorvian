using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IEmployeePayrollExemptionRepository
{
    Task<bool> IsExemptAsync(Guid employeeId, Guid conceptId);
}
