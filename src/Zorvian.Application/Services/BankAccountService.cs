using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class BankAccountService
{
    private readonly IBankAccountRepository _repo;

    public BankAccountService(IBankAccountRepository repo) => _repo = repo;

    public async Task<List<EmployeeBankAccountResponse>> GetByEmployeeIdAsync(Guid employeeId)
    {
        var items = await _repo.GetByEmployeeIdAsync(employeeId);
        return items.Select(Map).ToList();
    }

    public async Task<EmployeeBankAccountResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item != null ? Map(item) : null;
    }

    public async Task<EmployeeBankAccountResponse> CreateAsync(CreateEmployeeBankAccountRequest request)
    {
        var entity = new EmployeeBankAccount
        {
            EmployeeId = request.EmployeeId,
            BankName = request.BankName,
            AccountNumber = request.AccountNumber,
            AccountType = request.AccountType,
            AccountCurrency = request.AccountCurrency,
            IsDefault = request.IsDefault,
            IsActive = true
        };

        if (entity.IsDefault)
        {
            await DeactivateOtherDefaults(entity.EmployeeId);
        }

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<EmployeeBankAccountResponse?> UpdateAsync(Guid id, UpdateEmployeeBankAccountRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return null;

        if (request.BankName != null) entity.BankName = request.BankName;
        if (request.AccountNumber != null) entity.AccountNumber = request.AccountNumber;
        if (request.AccountType != null) entity.AccountType = request.AccountType;
        if (request.AccountCurrency != null) entity.AccountCurrency = request.AccountCurrency;
        if (request.IsDefault.HasValue) entity.IsDefault = request.IsDefault.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        if (request.IsDefault == true)
        {
            await DeactivateOtherDefaults(entity.EmployeeId);
        }

        await _repo.UpdateAsync(entity);
        await _repo.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        await _repo.DeleteAsync(id);
        await _repo.SaveChangesAsync();
        return true;
    }

    private async Task DeactivateOtherDefaults(Guid employeeId)
    {
        var accounts = await _repo.GetByEmployeeIdAsync(employeeId);
        foreach (var acc in accounts.Where(a => a.IsDefault))
        {
            acc.IsDefault = false;
            await _repo.UpdateAsync(acc);
        }
    }

    private static EmployeeBankAccountResponse Map(EmployeeBankAccount ba) => new(
        ba.Id, ba.EmployeeId, ba.BankName, ba.AccountNumber, ba.AccountType, ba.AccountCurrency, ba.IsDefault, ba.IsActive);
}
