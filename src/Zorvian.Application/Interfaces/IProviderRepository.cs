using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IProviderRepository
{
    Task<List<ServiceProvider>> GetProvidersAsync();
    Task<List<ServiceProvider>> GetProvidersByCountryAsync(string countryCode);
    Task<ServiceProvider?> GetProviderByIdAsync(Guid id);
    Task<ServiceProvider?> GetProviderByEmployeeIdAsync(Guid employeeId);
    Task AddProviderAsync(ServiceProvider provider);
    Task UpdateProviderAsync(ServiceProvider provider);
    Task DeleteProviderAsync(Guid id);
    Task<List<ServiceContract>> GetContractsByProviderIdAsync(Guid providerId);
    Task<List<ServiceContract>> GetAllContractsAsync();
    Task<List<ServiceContract>> GetContractsByCountryAsync(string countryCode);
    Task<ServiceContract?> GetContractByIdAsync(Guid id);
    Task AddContractAsync(ServiceContract contract);
    Task UpdateContractAsync(ServiceContract contract);
    Task DeleteContractAsync(Guid id);
    Task<List<PaymentMilestone>> GetMilestonesByContractIdAsync(Guid contractId);
    Task<PaymentMilestone?> GetMilestoneByIdAsync(Guid id);
    Task AddMilestoneAsync(PaymentMilestone milestone);
    Task UpdateMilestoneAsync(PaymentMilestone milestone);
    Task DeleteMilestoneAsync(Guid milestoneId);
    Task<List<ProviderInvoice>> GetInvoicesByMilestoneIdAsync(Guid milestoneId);
    Task<List<ProviderInvoice>> GetAllInvoicesAsync();
    Task<List<ProviderInvoice>> GetInvoicesByCountryAsync(string countryCode);
    Task<ProviderInvoice?> GetInvoiceByIdAsync(Guid id);
    Task AddInvoiceAsync(ProviderInvoice invoice);
    Task UpdateInvoiceAsync(ProviderInvoice invoice);
}
