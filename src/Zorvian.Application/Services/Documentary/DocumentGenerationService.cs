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
    private readonly ITenantContext _tenant;

    public DocumentGenerationService(
        IDocumentService documentService,
        IEmployeeRepository employeeRepo,
        ISaleRepository saleRepo,
        ICompanyRepository companyRepo,
        ITenantContext tenant)
    {
        _documentService = documentService;
        _employeeRepo = employeeRepo;
        _saleRepo = saleRepo;
        _companyRepo = companyRepo;
        _tenant = tenant;
    }

    private async Task<string> GetCompanyNameAsync()
    {
        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);
        return company?.Name ?? "Mi Empresa";
    }

    public async Task<GeneratedDocument> QuickGenerateEmployeeContractAsync(Guid employeeId, Guid templateId)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId);
        if (employee == null) throw new KeyNotFoundException("Employee not found");

        var companyName = await GetCompanyNameAsync();

        var data = new
        {
            Employee = new
            {
                FullName = $"{employee.FirstName} {employee.LastName}",
                Identification = employee.IdentificationNumber,
                Position = employee.Position,
                HireDate = employee.HireDate.ToString("dd/MM/yyyy"),
                Salary = (employee.Salary ?? 0).ToString("N2"),
                Address = employee.Address ?? "N/A"
            },
            Company = new
            {
                Name = companyName,
                Date = DateTime.UtcNow.ToString("dd/MM/yyyy")
            }
        };

        var doc = await _documentService.GenerateProfessionalDocumentAsync(templateId, employeeId, data);
        
        // Auto-finalize to be ready for signature immediately
        await _documentService.FinalizeAndRequestSignatureAsync(doc.Id, "Employee", employee.Id);
        
        return doc;
    }

    public async Task<GeneratedDocument> QuickGenerateSaleDocumentAsync(Guid saleId, Guid templateId)
    {
        var sale = await _saleRepo.GetByIdAsync(saleId);
        if (sale == null) throw new KeyNotFoundException("Sale not found");

        var companyName = await GetCompanyNameAsync();

        var data = new
        {
            Sale = new
            {
                Number = sale.InvoiceNumber,
                Total = sale.Total.ToString("N2"),
                Date = sale.SaleDate.ToString("dd/MM/yyyy"),
                ClientName = sale.Client != null ? $"{sale.Client.FirstName} {sale.Client.LastName}" : "General Public"
            },
            Company = new
            {
                Name = companyName
            }
        };

        var doc = await _documentService.GenerateProfessionalDocumentAsync(templateId, saleId, data);
        
        // Finalize for customer signature
        await _documentService.FinalizeAndRequestSignatureAsync(doc.Id, "Customer", sale.ClientId);

        return doc;
    }
}
