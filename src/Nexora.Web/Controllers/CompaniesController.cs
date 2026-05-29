using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Company;
using Nexora.Application.Services;

namespace Nexora.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/companies")]
public sealed class CompaniesController : ControllerBase
{
    private readonly CompanyService _companyService;

    public CompaniesController(CompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request)
    {
        try
        {
            var company = await _companyService.CreateAsync(request);
            return CreatedAtAction(nameof(GetCurrent), new { id = company.Id }, company);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var company = await _companyService.GetCurrentAsync();
        if (company is null)
            return NotFound(new { error = "Company not configured" });
        return Ok(company);
    }
}
