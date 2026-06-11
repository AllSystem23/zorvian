using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using ProviderEntity = Zorvian.Core.Entities.ServiceProvider;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/providers")]
public sealed class ProvidersController : ControllerBase
{
    private readonly ProviderService _service;

    public ProvidersController(ProviderService service) => _service = service;

    [Audit("Provider", "ReadList")]
    [HttpGet]
    [RequirePermission(Permissions.ProviderRead)]
    public async Task<IActionResult> GetProviders() =>
        Ok(await _service.GetProvidersAsync());

    [Audit("Provider", "Read")]
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.ProviderRead)]
    public async Task<IActionResult> GetProviderById(Guid id)
    {
        var result = await _service.GetProviderByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Provider", "Create")]
    [HttpPost]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> CreateProvider([FromBody] ProviderEntity provider)
    {
        var result = await _service.CreateProviderAsync(provider);
        return CreatedAtAction(nameof(GetProviderById), new { id = result.Id }, result);
    }

    [Audit("Provider", "Update")]
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> UpdateProvider(Guid id, [FromBody] ProviderEntity provider)
    {
        var result = await _service.UpdateProviderAsync(id, provider);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Provider", "Delete")]
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> DeleteProvider(Guid id) =>
        await _service.DeleteProviderAsync(id) ? NoContent() : NotFound();

    [Audit("Provider", "ReadContracts")]
    [HttpGet("{id:guid}/contracts")]
    [RequirePermission(Permissions.ProviderRead)]
    public async Task<IActionResult> GetContractsByProvider(Guid id) =>
        Ok(await _service.GetContractsByProviderAsync(id));

    [Audit("Provider", "ReadAllContracts")]
    [HttpGet("contracts")]
    [RequirePermission(Permissions.ProviderRead)]
    public async Task<IActionResult> GetAllContracts() =>
        Ok(await _service.GetAllContractsAsync());

    [Audit("Provider", "CreateContract")]
    [HttpPost("{id:guid}/contracts")]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> CreateContract(Guid id, [FromBody] ServiceContract contract)
    {
        contract.ServiceProviderId = id;
        var result = await _service.CreateContractAsync(contract);
        return CreatedAtAction(nameof(GetContractById), new { id = result.Id }, result);
    }

    [Audit("Provider", "ReadContract")]
    [HttpGet("contracts/{id:guid}")]
    [RequirePermission(Permissions.ProviderRead)]
    public async Task<IActionResult> GetContractById(Guid id)
    {
        var result = await _service.GetContractByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Provider", "UpdateContract")]
    [HttpPut("contracts/{id:guid}")]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> UpdateContract(Guid id, [FromBody] ServiceContract contract)
    {
        var result = await _service.UpdateContractAsync(id, contract);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Provider", "AddMilestone")]
    [HttpPost("contracts/{id:guid}/milestones")]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> AddMilestone(Guid id, [FromBody] PaymentMilestone milestone)
    {
        milestone.ServiceContractId = id;
        var result = await _service.CreateMilestoneAsync(milestone);
        return CreatedAtAction(nameof(GetContractById), new { id }, new { milestone = result });
    }

    [Audit("Provider", "ReadMilestones")]
    [HttpGet("contracts/{id:guid}/milestones")]
    [RequirePermission(Permissions.ProviderRead)]
    public async Task<IActionResult> GetMilestonesByContract(Guid id) =>
        Ok(await _service.GetMilestonesByContractAsync(id));

    [Audit("Provider", "ApproveMilestone")]
    [HttpPut("milestones/{id:guid}/approve")]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> ApproveMilestone(Guid id) =>
        await _service.ApproveMilestoneAsync(id) ? Ok(new { status = "approved" }) : NotFound();

    [Audit("Provider", "CompleteMilestone")]
    [HttpPut("milestones/{id:guid}/complete")]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> CompleteMilestone(Guid id) =>
        await _service.CompleteMilestoneAsync(id) ? Ok(new { status = "completed" }) : NotFound();

    [Audit("Provider", "RegisterInvoice")]
    [HttpPost("milestones/{id:guid}/invoice")]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> RegisterMilestoneInvoice(Guid id, [FromBody] ProviderInvoice invoice)
    {
        invoice.PaymentMilestoneId = id;
        var result = await _service.RegisterInvoiceAsync(invoice);
        return CreatedAtAction(nameof(GetInvoices), new { milestoneId = id }, result);
    }

    [Audit("Provider", "ReadInvoices")]
    [HttpGet("invoices")]
    [RequirePermission(Permissions.ProviderRead)]
    public async Task<IActionResult> GetInvoices() =>
        Ok(await _service.GetAllInvoicesAsync());

    [Audit("Provider", "PayInvoice")]
    [HttpPost("invoices/{id:guid}/pay")]
    [RequirePermission(Permissions.ProviderWrite)]
    public async Task<IActionResult> PayInvoice(Guid id)
    {
        var result = await _service.ProgramPaymentAsync(id, DateOnly.FromDateTime(DateTime.UtcNow), null);
        return result is null ? NotFound() : Ok(result);
    }
}
