using Zorvian.Application.DTOs.Document;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Documentary;

public sealed class DocumentGenerationService : IDocumentGenerationService
{
    private readonly IDocumentService _documentService;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ISaleRepository _saleRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IClientRepository _clientRepo;
    private readonly ITenantContext _tenant;

    public DocumentGenerationService(
        IDocumentService documentService,
        IEmployeeRepository employeeRepo,
        ISaleRepository saleRepo,
        ICompanyRepository companyRepo,
        IClientRepository clientRepo,
        ITenantContext tenant)
    {
        _documentService = documentService;
        _employeeRepo = employeeRepo;
        _saleRepo = saleRepo;
        _companyRepo = companyRepo;
        _clientRepo = clientRepo;
        _tenant = tenant;
    }

    public async Task<EntityContextResponse?> GetEntityContextAsync(string entityType, Guid entityId)
    {
        var data = new Dictionary<string, string>();
        string displayName;

        switch (entityType.ToLowerInvariant())
        {
            case "employee":
            {
                var employee = await _employeeRepo.GetByIdAsync(entityId);
                if (employee == null) return null;
                displayName = $"{employee.FirstName} {employee.LastName}";
                data["Employee.FullName"] = displayName;
                data["Employee.Identification"] = employee.IdentificationNumber ?? "";
                data["Employee.Position"] = employee.Position ?? "";
                data["Employee.HireDate"] = employee.HireDate.ToString("dd/MM/yyyy");
                data["Employee.Salary"] = (employee.Salary ?? 0).ToString("N2");
                data["Employee.Address"] = employee.Address ?? "";
                data["Employee.Email"] = employee.Email ?? "";
                data["Employee.Phone"] = employee.Phone ?? "";
                data["employee_name"] = displayName;
                data["employee_id"] = employee.IdentificationNumber ?? "";
                data["position"] = employee.Position ?? "";
                data["salary"] = (employee.Salary ?? 0).ToString("N2");
                data["start_date"] = employee.HireDate.ToString("dd/MM/yyyy");
                data["department"] = employee.Department?.Name ?? "";
                break;
            }

            case "sale":
            {
                var sale = await _saleRepo.GetByIdAsync(entityId);
                if (sale == null) return null;
                var clientName = sale.Client != null
                    ? $"{sale.Client.FirstName} {sale.Client.LastName}"
                    : "General Public";
                displayName = $"Venta {sale.InvoiceNumber} - {clientName}";
                data["Sale.Number"] = sale.InvoiceNumber ?? "";
                data["Sale.Total"] = sale.Total.ToString("N2");
                data["Sale.Date"] = sale.SaleDate.ToString("dd/MM/yyyy");
                data["Sale.ClientName"] = clientName;
                data["client_name"] = clientName;
                data["invoice_ref"] = sale.InvoiceNumber ?? "";
                break;
            }

            case "client":
            {
                var client = await _clientRepo.GetByIdAsync(entityId);
                if (client == null) return null;
                displayName = $"{client.FirstName} {client.LastName}";
                data["Client.FullName"] = displayName;
                data["Client.Identification"] = client.IdentificationNumber ?? "";
                data["Client.Phone"] = client.Phone ?? "";
                data["client_name"] = displayName;
                break;
            }

            default:
                return null;
        }

        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);
        var companyName = company?.Name ?? "Mi Empresa";
        data["Company.Name"] = companyName;
        data["Company.Date"] = DateTime.UtcNow.ToString("dd/MM/yyyy");
        data["Company.TaxId"] = company?.TaxId ?? "";

        return new EntityContextResponse(entityType, entityId, displayName, data);
    }

    public async Task<QuickGenerateResult> QuickGenerateAsync(string entityType, Guid entityId, Guid templateId)
    {
        var context = await GetEntityContextAsync(entityType, entityId);
        if (context == null)
            throw new KeyNotFoundException($"Entidad {entityType} con ID {entityId} no encontrada");

        var flatVars = context.Data;
        var doc = await _documentService.GenerateProfessionalDocumentAsync(templateId, entityId, flatVars);

        var signerRole = entityType.ToLowerInvariant() switch
        {
            "employee" => "Employee",
            "sale" => "Customer",
            "client" => "Customer",
            _ => "User"
        };

        await _documentService.FinalizeAndRequestSignatureAsync(doc.Id, signerRole, entityId);

        var signature = doc.Signatures.FirstOrDefault();
        return new QuickGenerateResult(
            doc.Id,
            doc.Name,
            doc.Status,
            doc.CreatedAt,
            PdfUrl: null,
            SignatureToken: signature?.SignatureToken
        );
    }

    public async Task<GeneratedDocument> QuickGenerateEmployeeContractAsync(Guid employeeId, Guid templateId)
    {
        var result = await QuickGenerateAsync("employee", employeeId, templateId);
        return (await _documentService.GetDocumentDetailsAsync(result.DocumentId))!;
    }

    public async Task<GeneratedDocument> QuickGenerateSaleDocumentAsync(Guid saleId, Guid templateId)
    {
        var result = await QuickGenerateAsync("sale", saleId, templateId);
        return (await _documentService.GetDocumentDetailsAsync(result.DocumentId))!;
    }
}
