using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zorvian.Infrastructure.Repositories;

public sealed class EmployeePayrollExemptionRepository : IEmployeePayrollExemptionRepository
{
    private readonly ZorvianDbContext _context;

    public EmployeePayrollExemptionRepository(ZorvianDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsExemptAsync(Guid employeeId, Guid conceptId)
    {
        return await _context.EmployeePayrollExemptions
            .AnyAsync(e => e.EmployeeId == employeeId 
                        && e.PayrollConceptId == conceptId 
                        && e.IsActive 
                        && (e.ExpiryDate == null || e.ExpiryDate >= DateTime.UtcNow));
    }
}
