using AutoMapper;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class ProviderService
{
    private readonly IProviderRepository _repo;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenant;

    public ProviderService(IProviderRepository repo, IMapper mapper, ITenantContext tenant)
    {
        _repo = repo;
        _mapper = mapper;
        _tenant = tenant;
    }

    public async Task<List<ServiceProvider>> GetProvidersAsync() =>
        await _repo.GetProvidersAsync();

    public async Task<ServiceProvider?> GetProviderByIdAsync(Guid id) =>
        await _repo.GetProviderByIdAsync(id);

    public async Task<ServiceProvider> CreateProviderAsync(ServiceProvider provider)
    {
        provider.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddProviderAsync(provider);
        return provider;
    }

    public async Task<ServiceProvider?> UpdateProviderAsync(Guid id, ServiceProvider provider)
    {
        var existing = await _repo.GetProviderByIdAsync(id);
        if (existing is null) return null;
        _mapper.Map(provider, existing);
        await _repo.UpdateProviderAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteProviderAsync(Guid id)
    {
        var existing = await _repo.GetProviderByIdAsync(id);
        if (existing is null) return false;
        await _repo.DeleteProviderAsync(id);
        return true;
    }

    public async Task<List<ServiceContract>> GetContractsByProviderAsync(Guid providerId) =>
        await _repo.GetContractsByProviderIdAsync(providerId);

    public async Task<List<ServiceContract>> GetAllContractsAsync() =>
        await _repo.GetAllContractsAsync();

    public async Task<List<PaymentMilestone>> GetMilestonesByContractAsync(Guid contractId) =>
        await _repo.GetMilestonesByContractIdAsync(contractId);

    public async Task<ServiceContract?> GetContractByIdAsync(Guid id) =>
        await _repo.GetContractByIdAsync(id);

    public async Task<ServiceContract> CreateContractAsync(ServiceContract contract)
    {
        contract.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddContractAsync(contract);
        return contract;
    }

    public async Task<ServiceContract?> UpdateContractAsync(Guid id, ServiceContract contract)
    {
        var existing = await _repo.GetContractByIdAsync(id);
        if (existing is null) return null;
        _mapper.Map(contract, existing);
        await _repo.UpdateContractAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteContractAsync(Guid id)
    {
        var existing = await _repo.GetContractByIdAsync(id);
        if (existing is null) return false;
        await _repo.DeleteContractAsync(id);
        return true;
    }

    public async Task<PaymentMilestone> CreateMilestoneAsync(PaymentMilestone milestone)
    {
        milestone.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddMilestoneAsync(milestone);
        return milestone;
    }

    public async Task<PaymentMilestone?> UpdateMilestoneAsync(Guid id, PaymentMilestone milestone)
    {
        var existing = (await _repo.GetMilestonesByContractIdAsync(milestone.ServiceContractId))
            .FirstOrDefault(m => m.Id == id);
        if (existing is null) return null;
        _mapper.Map(milestone, existing);
        await _repo.UpdateMilestoneAsync(existing);
        return existing;
    }

    public async Task<bool> ApproveMilestoneAsync(Guid milestoneId)
    {
        var milestone = await _repo.GetMilestoneByIdAsync(milestoneId);
        if (milestone is null) return false;
        milestone.Status = "approved";
        await _repo.UpdateMilestoneAsync(milestone);
        return true;
    }

    public async Task<bool> CompleteMilestoneAsync(Guid milestoneId)
    {
        var milestone = await _repo.GetMilestoneByIdAsync(milestoneId);
        if (milestone is null) return false;
        milestone.Status = "completed";
        milestone.CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _repo.UpdateMilestoneAsync(milestone);
        return true;
    }

    public async Task<bool> DeleteMilestoneAsync(Guid milestoneId)
    {
        await _repo.DeleteMilestoneAsync(milestoneId);
        return true;
    }

    public async Task<ProviderInvoice> RegisterInvoiceAsync(ProviderInvoice invoice)
    {
        invoice.NetAmount = invoice.InvoiceAmount - invoice.WithholdingAmount;
        invoice.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddInvoiceAsync(invoice);
        return invoice;
    }

    public async Task<List<ProviderInvoice>> GetAllInvoicesAsync() =>
        await _repo.GetAllInvoicesAsync();

    public async Task<ProviderInvoice?> ProgramPaymentAsync(Guid invoiceId, DateOnly paymentDate, string? paymentReference)
    {
        var invoice = await _repo.GetInvoiceByIdAsync(invoiceId);
        if (invoice is null) return null;

        invoice.Status = "scheduled";
        invoice.PaymentDate = paymentDate;
        invoice.PaymentReference = paymentReference;
        await _repo.UpdateInvoiceAsync(invoice);
        return invoice;
    }
}
