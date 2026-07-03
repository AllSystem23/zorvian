using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Permission;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de permisos laborales. Administra tipos de permiso, solicitudes, aprobación, rechazo y carga de documentos adjuntos.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/leave-permissions")]
public sealed class LeavePermissionsController : ControllerBase
{
    private readonly PermissionService _service;
    private readonly IDocumentStorageService _storage;

    public LeavePermissionsController(PermissionService service, IDocumentStorageService storage)
    {
        _service = service;
        _storage = storage;
    }

    /// <summary>
    /// Obtiene los tipos de permiso disponibles.
    /// </summary>
    [HttpGet("types")]
    public async Task<IActionResult> GetTypes()
    {
        var result = await _service.GetTypesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Crea una nueva solicitud de permiso.
    /// </summary>
    [Audit("Permission", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePermissionRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene una solicitud de permiso por su identificador.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null)
            return NotFound(new { error = "Permission request not found" });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene una lista filtrada de solicitudes de permiso.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] PermissionFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene las solicitudes de permiso del empleado autenticado.
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var result = await _service.GetMyAsync();
        return Ok(result);
    }

    /// <summary>
    /// Aprueba una solicitud de permiso. Requiere comentario opcional.
    /// </summary>
    [Audit("Permission", "Approve")]
    [HttpPut("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] CommentRequest body)
    {
        try
        {
            var result = await _service.ApproveAsync(id, body.Comment);
            if (result is null)
                return NotFound(new { error = "Permission request not found" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Rechaza una solicitud de permiso con un comentario.
    /// </summary>
    [Audit("Permission", "Reject")]
    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] CommentRequest body)
    {
        try
        {
            var result = await _service.RejectAsync(id, body.Comment ?? "");
            if (result is null)
                return NotFound(new { error = "Permission request not found" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sube un archivo adjunto (PDF, JPG, PNG) para una solicitud de permiso.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        if (!allowed.Contains(ext))
            return BadRequest(new { error = "Only PDF, JPG, and PNG files are allowed" });

        await using var stream = file.OpenReadStream();
        var relativePath = await _storage.UploadFileAsync(stream, $"permissions/{Guid.NewGuid()}{ext}", file.ContentType);

        return Ok(new
        {
            url = _storage.GetFileUrl(relativePath),
            fileName = file.FileName,
        });
    }
}
