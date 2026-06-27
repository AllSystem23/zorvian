using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class ProviderRepository : IProviderRepository
{
    private readonly ZorvianDbContext _db;

    public ProviderRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<ServiceProvider>> GetProvidersAsync() =>
        await _db.ServiceProviders
            .Include(p => p.Employee)
            .Include(p => p.Contracts)
            .ToListAsync();

    public async Task<List<ServiceProvider>> GetProvidersByCountryAsync(string countryCode) =>
        await _db.ServiceProviders
            .Include(p => p.Employee)
            .Include(p => p.Contracts)
            .Where(p => p.CountryCode == countryCode)
            .ToListAsync();

    public async Task<ServiceProvider?> GetProviderByIdAsync(Guid id) =>
        await _db.ServiceProviders
            .Include(p => p.Employee)
            .Include(p => p.Contracts)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<ServiceProvider?> GetProviderByEmployeeIdAsync(Guid employeeId) =>
        await _db.ServiceProviders
            .Include(p => p.Contracts)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId);

    public async Task AddProviderAsync(ServiceProvider provider) =>
        await _db.ServiceProviders.AddAsync(provider);

    public Task UpdateProviderAsync(ServiceProvider provider)
    {
        _db.ServiceProviders.Update(provider);
        return Task.CompletedTask;
    }

    public async Task DeleteProviderAsync(Guid id)
    {
        var provider = await _db.ServiceProviders.FindAsync(id);
        if (provider is not null)
            _db.ServiceProviders.Remove(provider);
    }

    public async Task<List<ServiceContract>> GetContractsByProviderIdAsync(Guid providerId) =>
        await _db.ServiceContracts
            .Include(c => c.Milestones)
            .Where(c => c.ServiceProviderId == providerId)
            .ToListAsync();

    public async Task<List<ServiceContract>> GetAllContractsAsync() =>
        await _db.ServiceContracts
            .Include(c => c.ServiceProvider)
            .Include(c => c.Milestones)
            .ToListAsync();

    public async Task<List<ServiceContract>> GetContractsByCountryAsync(string countryCode) =>
        await _db.ServiceContracts
            .Include(c => c.ServiceProvider)
            .Include(c => c.Milestones)
            .Where(c => c.CountryCode == countryCode)
            .ToListAsync();

    public async Task<ServiceContract?> GetContractByIdAsync(Guid id) =>
        await _db.ServiceContracts
            .Include(c => c.ServiceProvider)
            .Include(c => c.Milestones)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddContractAsync(ServiceContract contract) =>
        await _db.ServiceContracts.AddAsync(contract);

    public Task UpdateContractAsync(ServiceContract contract)
    {
        _db.ServiceContracts.Update(contract);
        return Task.CompletedTask;
    }

    public async Task DeleteContractAsync(Guid id)
    {
        var contract = await _db.ServiceContracts.FindAsync(id);
        if (contract is not null)
            _db.ServiceContracts.Remove(contract);
    }

    public async Task<List<PaymentMilestone>> GetMilestonesByContractIdAsync(Guid contractId) =>
        await _db.PaymentMilestones
            .Where(m => m.ServiceContractId == contractId)
            .OrderBy(m => m.EstimatedDate)
            .ToListAsync();

    public async Task<PaymentMilestone?> GetMilestoneByIdAsync(Guid id) =>
        await _db.PaymentMilestones.FindAsync(id);

    public async Task AddMilestoneAsync(PaymentMilestone milestone) =>
        await _db.PaymentMilestones.AddAsync(milestone);

    public Task UpdateMilestoneAsync(PaymentMilestone milestone)
    {
        _db.PaymentMilestones.Update(milestone);
        return Task.CompletedTask;
    }

    public async Task DeleteMilestoneAsync(Guid milestoneId)
    {
        var milestone = await _db.PaymentMilestones.FindAsync(milestoneId);
        if (milestone is not null)
            _db.PaymentMilestones.Remove(milestone);
    }

    public async Task<List<ProviderInvoice>> GetInvoicesByMilestoneIdAsync(Guid milestoneId) =>
        await _db.ProviderInvoices
            .Where(i => i.PaymentMilestoneId == milestoneId)
            .ToListAsync();

    public async Task<List<ProviderInvoice>> GetAllInvoicesAsync() =>
        await _db.ProviderInvoices
            .Include(i => i.PaymentMilestone)
            .ThenInclude(m => m.ServiceContract)
            .ThenInclude(c => c.ServiceProvider)
            .ToListAsync();

    public async Task<List<ProviderInvoice>> GetInvoicesByCountryAsync(string countryCode) =>
        await _db.ProviderInvoices
            .Include(i => i.PaymentMilestone)
            .ThenInclude(m => m.ServiceContract)
            .ThenInclude(c => c.ServiceProvider)
            .Where(i => i.PaymentMilestone.ServiceContract.CountryCode == countryCode)
            .ToListAsync();

    public async Task<ProviderInvoice?> GetInvoiceByIdAsync(Guid id) =>
        await _db.ProviderInvoices.FindAsync(id);

    public async Task AddInvoiceAsync(ProviderInvoice invoice) =>
        await _db.ProviderInvoices.AddAsync(invoice);

    public Task UpdateInvoiceAsync(ProviderInvoice invoice)
    {
        _db.ProviderInvoices.Update(invoice);
        return Task.CompletedTask;
    }
}
