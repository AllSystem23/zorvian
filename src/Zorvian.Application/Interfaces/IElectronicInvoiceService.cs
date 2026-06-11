using Zorvian.Application.DTOs;

namespace Zorvian.Application.Interfaces;

public interface IElectronicInvoiceService
{
    Task<ElectronicInvoiceDto> IssueAsync(Guid saleId, string countryCode);
    Task<ElectronicInvoiceDto?> GetBySaleAsync(Guid saleId);
    Task<ElectronicInvoiceDto?> GetByIdAsync(Guid id);
    Task<List<ElectronicInvoiceDto>> GetByCompanyAsync(Guid companyId, string? countryCode = null);
    Task<ElectronicInvoiceDto> ResubmitAsync(Guid id);
    Task CancelAsync(Guid id, string reason);
    Task<string> GenerateXmlAsync(Guid saleId, string countryCode);
    Task<string> GeneratePdfAsync(Guid id);
}
