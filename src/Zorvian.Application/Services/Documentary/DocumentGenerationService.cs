using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.Documentary;

public sealed class DocumentGenerationService : IDocumentGenerationService
{
    private readonly IDocumentService _documentService;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ISaleRepository _saleRepo;

    public DocumentGenerationService(
        IDocumentService documentService,
        IEmployeeRepository employeeRepo,
        ISaleRepository saleRepo)
    {
        _documentService = documentService;
        _employeeRepo = employeeRepo;
        _saleRepo = saleRepo;
    }

    public async Task<GeneratedDocument> QuickGenerateEmployeeContractAsync(Guid employeeId, Guid templateId)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId);
        if (employee == null) throw new ArgumentException("Employee not found");

        // Logic for 3-click rule: Prepare data automatically
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
                Name = "Zorvian ERP Demo",
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
        if (sale == null) throw new ArgumentException("Sale not found");

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
                Name = "Zorvian ERP Commercial"
            }
        };

        var doc = await _documentService.GenerateProfessionalDocumentAsync(templateId, saleId, data);
        
        // Finalize for customer signature
        await _documentService.FinalizeAndRequestSignatureAsync(doc.Id, "Customer", sale.ClientId);

        return doc;
    }
}
