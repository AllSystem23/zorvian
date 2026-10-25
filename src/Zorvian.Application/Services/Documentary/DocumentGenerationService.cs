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

    public async Task<EntityContextResponse?> GetEntityContextAsync(string entityType, Guid entityId)
    {
        var context = await _contextBuilder.BuildAsync(entityType, entityId);
        if (context == null) return null;

        // La "Liquidación Final" es una plantilla del MOTOR DOCUMENTAL, no solo
        // un certificado: el builder de entidades solo expone las ~15 claves
        // básicas del empleado. Sin este paso, el wizard del Centro Documental
        // preveía (GET entity-context) y generaba (POST quick-generate) el PDF
        // con las ~50 variables de liquidación vacías.
        if (string.Equals(entityType, "employee", StringComparison.OrdinalIgnoreCase))
        {
            var employee = await _employeeRepo.GetByIdWithBranchAsync(entityId);
            var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);
            if (employee != null && company != null)
                await EnrichLiquidacionContextAsync(context, employee, company);
        }

        return context;
    }

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
                // Las variables de liquidación ya vienen calculadas del contexto
                // de entidad: GetEntityContextAsync las enriquece para todo
                // empleado (evita calcularlas dos veces).
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
        EntityContextResponse context, Employee employee, Company company)
    {
        var liquidationVars = await _certificateCalc.CalculateLiquidationVariablesAsync(
            employee.Id,
            employee.Salary ?? 0,
            employee.HireDate,
            employee.TerminationDate,
            employee.TerminationReason,
            FiscalYearHelper.MapCountryToCode(company.Country));

        foreach (var (key, value) in liquidationVars)
            context.Data[key] = value;

        // LegalName es string.Empty por defecto (no null) → el ?? no bastaba.
        context.Data["autorizado_por"] =
            string.IsNullOrWhiteSpace(company.LegalName) ? company.Name ?? "" : company.LegalName;
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

    public async Task<QuickGenerateResult> QuickGenerateAsync(
        string entityType, Guid entityId, Guid templateId, Dictionary<string, string>? variables = null)
    {
        var context = await GetEntityContextAsync(entityType, entityId);
        if (context == null)
            throw new KeyNotFoundException($"Entidad {entityType} con ID {entityId} no encontrada");

        var flatVars = context.Data;
        if (variables != null)
        {
            // Lo editado en el formulario previo (paso 2 del wizard) manda sobre
            // el contexto calculado — mismo merge que hace POST /documents/generate.
            foreach (var kv in variables)
                flatVars[kv.Key] = kv.Value;
        }
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
