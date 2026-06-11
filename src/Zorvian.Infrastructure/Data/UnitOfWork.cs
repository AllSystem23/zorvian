using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ZorvianDbContext _db;

    public UnitOfWork(ZorvianDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}
