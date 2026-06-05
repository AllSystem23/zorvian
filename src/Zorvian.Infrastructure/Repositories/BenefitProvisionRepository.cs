using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class BenefitProvisionRepository : IBenefitProvisionRepository
{
    private readonly ZorvianDbContext _db;

    public BenefitProvisionRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<BenefitProvision>> GetByEmployeeAsync(Guid employeeId) =>
        await _db.BenefitProvisions
            .Where(bp => bp.EmployeeId == employeeId)
            .ToListAsync();

    public async Task AddAsync(BenefitProvision provision) =>
        await _db.BenefitProvisions.AddAsync(provision);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
