using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IBenefitProvisionRepository
{
    Task<List<BenefitProvision>> GetByEmployeeAsync(Guid employeeId);
    Task AddAsync(BenefitProvision provision);
    Task SaveChangesAsync();
}
