using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TreasuryController : ControllerBase
    {
        private readonly ITreasuryService _treasuryService;

        public TreasuryController(ITreasuryService treasuryService)
        {
            _treasuryService = treasuryService;
        }

        [HttpPost("issue")]
        public async Task<IActionResult> IssueCheck([FromBody] Check check)
        {
            // Nota: En un entorno real, obtener el UserId del contexto de seguridad (User.FindFirst)
            var userId = Guid.NewGuid(); 
            var issuedCheck = await _treasuryService.IssueCheckAsync(check, userId);
            return Ok(issuedCheck);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] CheckStatus newStatus, [FromQuery] string? remarks)
        {
            var userId = Guid.NewGuid();
            await _treasuryService.UpdateCheckStatusAsync(id, newStatus, userId, remarks);
            return NoContent();
        }

        [HttpGet("template/{bankId}")]
        public async Task<IActionResult> GetTemplate(Guid bankId)
        {
            var template = await _treasuryService.GetPrintTemplateAsync(bankId);
            return template != null ? Ok(template) : NotFound();
        }
    }
}
