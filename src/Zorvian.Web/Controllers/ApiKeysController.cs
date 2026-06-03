using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Infrastructure.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,CompanyAdmin")]
[Route("api/v1/api-keys")]
public sealed class ApiKeysController : ControllerBase
{
    private readonly ApiKeyService _service;
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public ApiKeysController(ApiKeyService service, ZorvianDbContext db, ITenantContext tenant)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetKeys()
    {
        var keys = await _db.Set<ApiKey>()
            .Select(k => new { k.Id, k.Name, k.Prefix, k.LastUsedAt, k.ExpiresAt, k.IsActive, k.CreatedAt })
            .ToListAsync();
        return Ok(keys);
    }

    [HttpPost]
    public async Task<IActionResult> CreateKey([FromBody] CreateApiKeyRequest request)
    {
        var (key, id) = await _service.CreateApiKeyAsync(request.Name, _tenant.TenantId, request.ExpiresAt);
        return Ok(new { id, key, message = "Guarda esta clave ahora, no se volverá a mostrar." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteKey(Guid id)
    {
        var key = await _db.Set<ApiKey>().FindAsync(id);
        if (key == null) return NotFound();

        _db.Set<ApiKey>().Remove(key);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateApiKeyRequest(string Name, DateTime? ExpiresAt);
