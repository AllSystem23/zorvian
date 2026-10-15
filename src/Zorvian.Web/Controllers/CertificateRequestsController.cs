using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/[controller]")]
public sealed class CertificateRequestsController : ControllerBase
{
    private readonly CertificateRequestService _service;
    private readonly ITenantContext _tenant;
    private readonly ZorvianDbContext _db;
    private readonly QuestPdfService _pdfService;

    public CertificateRequestsController(
        CertificateRequestService service,
        ITenantContext tenant,
        ZorvianDbContext db,
        QuestPdfService pdfService)
    {
        _service = service;
        _tenant = tenant;
        _db = db;
        _pdfService = pdfService;
    }

    /// <summary>El trabajador crea una solicitud de constancia salarial o laboral.</summary>
    [Audit("CertificateRequest", "Create")]
    [HttpPost("employees/me/certificate-requests")]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> CreateMyRequest([FromBody] CreateCertificateRequestDto dto)
    {
        if (_tenant.CurrentEmployeeId is null)
            return Unauthorized(new { error = "No employee profile linked" });

        try
        {
            var result = await _service.CreateAsync(_tenant.CurrentEmployeeId.Value, dto);
            return CreatedAtAction(nameof(GetMyRequests), new { }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>El trabajador consulta sus propias solicitudes.</summary>
    [HttpGet("employees/me/certificate-requests")]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> GetMyRequests()
    {
        if (_tenant.CurrentEmployeeId is null)
            return Unauthorized(new { error = "No employee profile linked" });

        var results = await _service.GetMineAsync(_tenant.CurrentEmployeeId.Value);
        return Ok(results);
    }

    /// <summary>Descarga el PDF de una solicitud aprobada (solo el propio trabajador).</summary>
    [HttpGet("employees/me/certificate-requests/{id:guid}/pdf")]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> DownloadMyCertificatePdf(Guid id)
    {
        if (_tenant.CurrentEmployeeId is null)
            return Unauthorized(new { error = "No employee profile linked" });

        var request = await _db.CertificateRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null || request.EmployeeId != _tenant.CurrentEmployeeId.Value)
            return NotFound(new { error = "Solicitud no encontrada" });

        if (request.Status != "approved" || request.GeneratedDocumentId is null)
            return BadRequest(new { error = "La solicitud aún no ha sido aprobada" });

        var doc = await _db.GeneratedDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == request.GeneratedDocumentId.Value);

        if (doc is null || doc.Versions.Count == 0)
            return NotFound(new { error = "Documento no encontrado" });

        var latestVersion = doc.Versions.OrderByDescending(v => v.VersionNumber).First();
        var pdfBytes = _pdfService.RenderHtmlToPdf(latestVersion.Content);

        var fileName = $"constancia_{request.CertificateType}_{request.Employee.FirstName}_{request.Employee.LastName}.pdf"
            .Replace(" ", "_");

        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>RRHH: lista todas las solicitudes (opcionalmente filtradas por empleado/estado).</summary>
    [HttpGet]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? employeeId, [FromQuery] string? status)
    {
        if (!IsHrOrAdmin())
            return Forbid();

        var results = await _service.GetAllAsync(employeeId, status);
        return Ok(results);
    }

    /// <summary>RRHH: aprueba la solicitud y genera el documento PDF.</summary>
    [Audit("CertificateRequest", "Approve")]
    [HttpPost("{id:guid}/approve")]
    [RequirePermission(Permissions.EmployeeWrite)]
    public async Task<IActionResult> Approve(Guid id)
    {
        if (!IsHrOrAdmin())
            return Forbid();

        try
        {
            var result = await _service.ApproveAsync(id, _tenant.CurrentUserId ?? Guid.Empty);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>RRHH: rechaza la solicitud.</summary>
    [Audit("CertificateRequest", "Reject")]
    [HttpPost("{id:guid}/reject")]
    [RequirePermission(Permissions.EmployeeWrite)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequestDto dto)
    {
        if (!IsHrOrAdmin())
            return Forbid();

        try
        {
            var result = await _service.RejectAsync(id, dto.RejectionReason ?? "Sin motivo", _tenant.CurrentUserId ?? Guid.Empty);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool IsHrOrAdmin() =>
        User.IsInRole("Rrhh") || User.IsInRole("CompanyAdmin") || User.IsInRole("SuperAdmin");
}

public record RejectRequestDto(string? RejectionReason);
