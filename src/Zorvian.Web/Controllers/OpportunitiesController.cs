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
[Route("zorvian/v1/crm/opportunities")]
public sealed class OpportunitiesController : ControllerBase
{
    private readonly OpportunityService _service;
    private readonly IMapper _mapper;

    public OpportunitiesController(OpportunityService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [Audit("Opportunity", "ReadList")]
    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var opps = await _service.GetActiveOpportunitiesAsync();
        return Ok(_mapper.Map<List<OpportunityResponse>>(opps));
    }

    [Audit("Opportunity", "Read")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var opp = await _service.GetOpportunityByIdAsync(id);
        if (opp == null) return NotFound();
        return Ok(_mapper.Map<OpportunityResponse>(opp));
    }

    [Audit("Opportunity", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityRequest request)
    {
        var opp = _mapper.Map<Opportunity>(request);
        var created = await _service.CreateOpportunityAsync(opp);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<OpportunityResponse>(created));
    }

    [Audit("Opportunity", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOpportunityRequest request)
    {
        var opp = await _service.GetOpportunityByIdAsync(id);
        if (opp == null) return NotFound();

        _mapper.Map(request, opp);
        await _service.UpdateOpportunityAsync(opp);
        return Ok(_mapper.Map<OpportunityResponse>(opp));
    }
    
    [Audit("Opportunity", "ReadByStage")]
    [HttpGet("stage/{stageId:guid}")]
    public async Task<IActionResult> GetByStage(Guid stageId)
    {
        var opps = await _service.GetByPipelineStageAsync(stageId);
        return Ok(_mapper.Map<List<OpportunityResponse>>(opps));
    }
}
