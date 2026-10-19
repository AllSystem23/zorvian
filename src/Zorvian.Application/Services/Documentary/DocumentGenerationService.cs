using Zorvian.Application.DTOs.Document;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Documentary;

/// <summary>
/// Orquestador del motor documental: contexto de entidades (delegado a
/// <see cref="IEntityDocumentContextBuilder"/>), contextos de certificados,
/// generación rápida y firmas. La construcción de datos por tipo de entidad
/// vive en <see cref="EntityDocumentContextBuilder"/> para no acumular
/// dependencias de repositorio en este servicio.
/// </summary>
public sealed class DocumentGenerationService : IDocumentGenerationService
{
    private readonly IDocumentService _documentService;
    private readonly IEntityDocumentContextBuilder _contextBuilder;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ICertificateCalculator _certificateCalc;
    private readonly ITenantContext _tenant;

    public DocumentGenerationService(
        IDocumentService documentService,
        IEntityDocumentContextBuilder contextBuilder,
        IEmployeeRepository employeeRepo,
        ICompanyRepository companyRepo,
        ICertificateCalculator certificateCalc,
        ITenantContext tenant)
    {
        _documentService = documentService;
        _contextBuilder = contextBuilder;
        _employeeRepo = employeeRepo;
        _companyRepo = companyRepo;
        _certificateCalc = certificateCalc;
        _tenant = tenant;
    }

    public Task<EntityContextResponse?> GetEntityContextAsync(string entityType, Guid entityId)
        => _contextBuilder.BuildAsync(entityType, entityId);

    /// <summary>
    /// Obtiene el contexto completo para un certificado específico, calculando
    /// todas las variables necesarias según el tipo de documento.
    /// </summary>
    public async Task<Dictionary<string, string>> GetCertificateContextAsync(
        string certificateType, Guid employeeId)
    {
        var employee = await _employeeRepo.GetByIdWithBranchAsync(employeeId)
            ?? throw new KeyNotFoundException("Empleado no encontrado");

        var entityContext = await GetEntityContextAsync("employee", employeeId)
            ?? throw new InvalidOperationException("No se pudo obtener el contexto del empleado");

        // El contexto de entidades expone Dictionary<string,object> (admite listas de
        // ítems); los certificados son siempre valores escalares → aplanar a string.
        var data = entityContext.Data.ToDictionary(
            kv => kv.Key, kv => kv.Value?.ToString() ?? "", StringComparer.Ordinal);

        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId)
            ?? throw new InvalidOperationException("Compañía no encontrada");

        switch (certificateType.ToLowerInvariant())
        {
            case "salarial":
            case "laboral":
                EnrichConstanciaContext(data, employee, company);
                break;
            case "contrato":
                EnrichContratoContext(data, employee, company);
                break;
            case "liquidacion":
                await EnrichLiquidacionContextAsync(data, employee, company);
                break;
            case "poder":
                EnrichPoderContext(data, employee, company);
                break;
        }

        return data;
    }

    private static void EnrichConstanciaContext(
        Dictionary<string, string> data, Employee employee, Company company)
    {
        data["fecha_emision"] = DateTime.Now.ToString("dd/MM/yyyy");
        data["contract_type"] = "Término Indefinido";
        data["purpose"] = data.GetValueOrDefault("purpose", "Trámite general");
    }

    private static void EnrichContratoContext(
        Dictionary<string, string> data, Employee employee, Company company)
    {
        data["contract_type"] = "Término Indefinido";
        data["salary_type"] = employee.SalaryType == "monthly" ? "Mensual" : "Quincenal";
        data["fecha_inicio"] = employee.HireDate.ToString("dd/MM/yyyy");
        data["trial_period"] = "3 meses";
        data["work_schedule"] = "Lunes a Viernes 8:00am - 5:00pm";
        data["benefits"] = "Seguro Social (INSS), Aguinaldo, Vacaciones (15 días)";
    }

    private async Task EnrichLiquidacionContextAsync(
        Dictionary<string, string> data, Employee employee, Company company)
    {
        var liquidationVars = await _certificateCalc.CalculateLiquidationVariablesAsync(
            employee.Id,
            employee.Salary ?? 0,
            employee.HireDate,
            employee.TerminationDate,
            employee.TerminationReason,
            FiscalYearHelper.MapCountryToCode(company.Country));

        foreach (var (key, value) in liquidationVars)
            data[key] = value;

        data["autorizado_por"] = company.LegalName ?? company.Name ?? "";
    }

    private void EnrichPoderContext(
        Dictionary<string, string> data, Employee employee, Company company)
    {
        var poderVars = _certificateCalc.CalculatePowerOfAttorneyVariables(
            company.LegalName ?? company.Name ?? "La Empresa",
            company.TaxId,
            $"{employee.FirstName} {employee.LastName}",
            employee.IdentificationNumber);

        foreach (var (key, value) in poderVars)
            data[key] = value;
    }

    public async Task<QuickGenerateResult> QuickGenerateAsync(string entityType, Guid entityId, Guid templateId)
    {
        var context = await GetEntityContextAsync(entityType, entityId);
        if (context == null)
            throw new KeyNotFoundException($"Entidad {entityType} con ID {entityId} no encontrada");

        var flatVars = context.Data;
        var doc = await _documentService.GenerateProfessionalDocumentAsync(templateId, entityId, flatVars, entityType);

        var signerRole = entityType.ToLowerInvariant() switch
        {
            "employee" => "Employee",
            "sale" => "Customer",
            "client" => "Customer",
            _ => "User"
        };

        await _documentService.FinalizeAndRequestSignatureAsync(doc.Id, signerRole, entityId);

        var fresh = await _documentService.GetDocumentDetailsAsync(doc.Id);
        var signature = fresh?.Signatures.FirstOrDefault();
        return new QuickGenerateResult(
            doc.Id,
            doc.Name,
            fresh?.Status ?? doc.Status,
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
