using Zorvian.Application.DTOs.Commercial;

namespace Zorvian.Application.Interfaces;

public interface IPurchaseIntelligenceService
{
    Task<CreatePurchaseRequest> AnalyzeInvoiceAsync(Stream fileStream);
}
