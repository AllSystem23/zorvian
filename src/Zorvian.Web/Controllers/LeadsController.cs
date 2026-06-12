using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/crm/leads")]
public sealed class LeadsController : ControllerBase
{
    private readonly LeadService _service;
    private readonly IMapper _mapper;

    public LeadsController(LeadService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [Audit("Lead", "ReadList")]
    [HttpGet]
    public async Task<IActionResult> GetLeads([FromQuery] LeadFilterRequest filter)
    {
        var (leads, total) = await _service.GetLeadsAsync(filter.Search, filter.Status, filter.Page, filter.PageSize);
        var response = _mapper.Map<List<LeadResponse>>(leads);
        return Ok(new { data = response, total });
    }

    [Audit("Lead", "Read")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var lead = await _service.GetLeadByIdAsync(id);
        if (lead == null) return NotFound();
        return Ok(_mapper.Map<LeadResponse>(lead));
    }

    [Audit("Lead", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeadRequest request)
    {
        var lead = _mapper.Map<Lead>(request);
        var created = await _service.CreateLeadAsync(lead);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<LeadResponse>(created));
    }

    [Audit("Lead", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadRequest request)
    {
        var lead = await _service.GetLeadByIdAsync(id);
        if (lead == null) return NotFound();
        
        _mapper.Map(request, lead);
        await _service.UpdateLeadAsync(lead);
        return Ok(_mapper.Map<LeadResponse>(lead));
    }

    [Audit("Lead", "Delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteLeadAsync(id);
        return NoContent();
    }
}
