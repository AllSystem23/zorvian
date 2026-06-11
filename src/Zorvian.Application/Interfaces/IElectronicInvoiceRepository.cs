using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IElectronicInvoiceRepository
{
    Task<ElectronicInvoice?> GetByIdAsync(Guid id);
    Task<ElectronicInvoice?> GetBySaleAsync(Guid saleId);
    Task<List<ElectronicInvoice>> GetByCompanyAsync(Guid companyId, string? countryCode = null);
    Task AddAsync(ElectronicInvoice invoice);
    Task UpdateAsync(ElectronicInvoice invoice);
    Task AddXmlAsync(ElectronicInvoiceXml xml);
    Task<List<ElectronicInvoiceXml>> GetXmlsAsync(Guid invoiceId);
    Task<ElectronicInvoiceSummaryDto> GetSummaryAsync(Guid companyId, string? countryCode = null);
    Task SaveChangesAsync();
}

public sealed record ElectronicInvoiceSummaryDto(
    int Total,
    int Authorized,
    int Pending,
    int Rejected,
    int Cancelled
);
