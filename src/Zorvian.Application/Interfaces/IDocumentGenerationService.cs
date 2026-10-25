using Zorvian.Application.DTOs.Document;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IDocumentGenerationService
{
    Task<GeneratedDocument> QuickGenerateEmployeeContractAsync(Guid employeeId, Guid templateId);
    Task<GeneratedDocument> QuickGenerateSaleDocumentAsync(Guid saleId, Guid templateId);
    Task<QuickGenerateResult> QuickGenerateAsync(
        string entityType, Guid entityId, Guid templateId, Dictionary<string, string>? variables = null);
    Task<EntityContextResponse?> GetEntityContextAsync(string entityType, Guid entityId);
    Task<Dictionary<string, string>> GetCertificateContextAsync(string certificateType, Guid employeeId);
}
