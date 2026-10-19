using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.ML;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Asistente Z-IA (chat) e ingesta de conocimiento.
/// `ask` queda SIN permiso de módulo a propósito: el chat es transversal a
/// todos los roles autenticados (no existe un permiso "chat" en el catálogo).
/// `seed-knowledge` sí es una operación administrativa → `settings.write`.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IPolicyService _policyService;
    private readonly ITenantContext _tenant;

    public ChatController(IChatService chatService, IPolicyService policyService, ITenantContext tenant)
    {
        _chatService = chatService;
        _policyService = policyService;
        _tenant = tenant;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        var answer = await _chatService.ChatAsync(_tenant.TenantId, request.Question);
        return Ok(new ChatResponseDto { Answer = answer });
    }

    [HttpPost("seed-knowledge")]
    [RequirePermission(Permissions.SettingsWrite)]
    public async Task<IActionResult> SeedKnowledge([FromBody] SeedKnowledgeRequestDto request)
    {
        await _policyService.IngestDocumentAsync(request.Title, request.Content);
        return Ok(new { message = "Knowledge document ingested successfully." });
    }
}

public sealed class SeedKnowledgeRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
