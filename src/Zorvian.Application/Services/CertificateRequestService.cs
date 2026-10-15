using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.Documentary;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class CertificateRequestService
{
    private readonly ICertificateRequestRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDocumentGenerationService _docGeneration;
    private readonly IDocumentService _docService;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly ITenantContext _tenant;
    private readonly ILogger<CertificateRequestService> _logger;

    public CertificateRequestService(
        ICertificateRequestRepository repo,
        IEmployeeRepository employeeRepo,
        IDocumentGenerationService docGeneration,
        IDocumentService docService,
        IDocumentTemplateRepository templateRepo,
        ITenantContext tenant,
        ILogger<CertificateRequestService> logger)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _docGeneration = docGeneration;
        _docService = docService;
        _templateRepo = templateRepo;
        _tenant = tenant;
        _logger = logger;
    }

    /// <summary>El trabajador crea una solicitud de constancia.</summary>
    public async Task<CertificateRequestResponse> CreateAsync(Guid employeeId, CreateCertificateRequestDto dto)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId)
            ?? throw new KeyNotFoundException("Empleado no encontrado");

        var request = new CertificateRequest
        {
            EmployeeId = employeeId,
            CertificateType = NormalizeCertificateType(dto.CertificateType),
            Reason = dto.Reason,
            Status = "pending",
            RequestedBy = _tenant.CurrentUserId?.ToString() ?? employeeId.ToString(),
            RequestedAt = DateTime.UtcNow,
            TenantId = _tenant.TenantId,
            CompanyId = _tenant.EffectiveCompanyId ?? Guid.Empty
        };

        await _repo.AddAsync(request);
        await _repo.SaveChangesAsync();

        return MapToResponse(request, employee);
    }

    /// <summary>El trabajador consulta sus propias solicitudes.</summary>
    public async Task<List<CertificateRequestResponse>> GetMineAsync(Guid employeeId)
    {
        var requests = await _repo.GetByEmployeeAsync(employeeId);
        return requests.Select(r => MapToResponse(r, r.Employee)).ToList();
    }

    /// <summary>RRHH: lista todas las solicitudes (opcionalmente filtradas).</summary>
    public async Task<List<CertificateRequestResponse>> GetAllAsync(Guid? employeeId = null, string? status = null)
    {
        var requests = await _repo.GetAllAsync(employeeId, status);
        return requests.Select(r => MapToResponse(r, r.Employee)).ToList();
    }

    /// <summary>RRHH: aprueba la solicitud y genera el documento PDF.</summary>
    public async Task<CertificateRequestResponse> ApproveAsync(Guid requestId, Guid processedBy)
    {
        var request = await _repo.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Solicitud no encontrada");

        if (request.Status != "pending")
            throw new InvalidOperationException($"La solicitud ya fue procesada (estado: {request.Status})");

        var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId)
            ?? throw new KeyNotFoundException("Empleado no encontrado");

        var generatedDoc = await GenerateCertificateDocumentAsync(employee, request);

        request.Status = "approved";
        request.GeneratedDocumentId = generatedDoc.Id;
        request.ProcessedBy = processedBy;
        request.ProcessedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(request);
        await _repo.SaveChangesAsync();

        _logger.LogInformation(
            "Certificate request {RequestId} approved for employee {EmployeeId}. Document {DocumentId} generated.",
            requestId, request.EmployeeId, generatedDoc.Id);

        return MapToResponse(request, employee);
    }

    /// <summary>RRHH: rechaza la solicitud.</summary>
    public async Task<CertificateRequestResponse> RejectAsync(Guid requestId, string rejectionReason, Guid processedBy)
    {
        var request = await _repo.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Solicitud no encontrada");

        if (request.Status != "pending")
            throw new InvalidOperationException($"La solicitud ya fue procesada (estado: {request.Status})");

        var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId)
            ?? throw new KeyNotFoundException("Empleado no encontrado");

        request.Status = "rejected";
        request.RejectionReason = rejectionReason;
        request.ProcessedBy = processedBy;
        request.ProcessedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(request);
        await _repo.SaveChangesAsync();

        return MapToResponse(request, employee);
    }

    /// <summary>Genera el documento PDF de la constancia usando el motor documental.</summary>
    private async Task<GeneratedDocument> GenerateCertificateDocumentAsync(Employee employee, CertificateRequest request)
    {
        var templateName = ResolveTemplateName(request.CertificateType);

        var template = await _templateRepo.GetByNameAsync(templateName)
            ?? throw new InvalidOperationException($"Plantilla '{templateName}' no encontrada. Verifique el seed.");

        var context = await _docGeneration.GetCertificateContextAsync(request.CertificateType, employee.Id);

        return await _docService.GenerateProfessionalDocumentAsync(
            template.Id,
            employee.Id,
            context,
            "employee");
    }

    private static string NormalizeCertificateType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "laboral" => "laboral",
            "contrato" => "contrato",
            "liquidacion" => "liquidacion",
            "poder" => "poder",
            _ => "salarial"
        };

    private static string ResolveTemplateName(string certificateType) =>
        certificateType switch
        {
            "laboral" => "Constancia de Empleo",
            "salarial" => "Constancia de Empleo",
            "contrato" => "Contrato de Trabajo a Término Indefinido",
            "liquidacion" => "Liquidación Final (Nicaragua)",
            "poder" => "Poder Notarial General",
            _ => "Constancia de Empleo"
        };

    private static CertificateRequestResponse MapToResponse(CertificateRequest r, Employee e)
    {
        return new CertificateRequestResponse(
            r.Id,
            r.CertificateType,
            r.Reason,
            r.Status,
            r.RejectionReason,
            r.GeneratedDocumentId,
            r.RequestedAt,
            r.ProcessedAt,
            $"{e.FirstName} {e.LastName}",
            e.IdentificationNumber ?? "");
    }
}
