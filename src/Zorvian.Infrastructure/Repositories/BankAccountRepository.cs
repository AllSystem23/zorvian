using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class BankAccountRepository : IBankAccountRepository
{
    private readonly ZorvianDbContext _db;

    public BankAccountRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<EmployeeBankAccount>> GetByEmployeeIdAsync(Guid employeeId) =>
        await _db.EmployeeBankAccounts
            .Where(ba => ba.EmployeeId == employeeId)
            .ToListAsync();

    public async Task<EmployeeBankAccount?> GetByIdAsync(Guid id) =>
        await _db.EmployeeBankAccounts.FindAsync(id);

    public async Task AddAsync(EmployeeBankAccount account) =>
        await _db.EmployeeBankAccounts.AddAsync(account);

    public Task UpdateAsync(EmployeeBankAccount account)
    {
        _db.EmployeeBankAccounts.Update(account);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.EmployeeBankAccounts.FindAsync(id);
        if (entity != null) _db.EmployeeBankAccounts.Remove(entity);
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
