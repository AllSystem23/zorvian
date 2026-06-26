using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;
using AutoMapper;
using Zorvian.Application.DTOs.Warranty;

namespace Zorvian.Web.Controllers;

[ApiController]
[Route("zorvian/v1/public/warranties")]
public sealed class PublicWarrantiesController : ControllerBase
{
    private readonly IWarrantyRepository _repo;
    private readonly IMapper _mapper;

    public PublicWarrantiesController(IWarrantyRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    [HttpGet("track/{warrantyNumber}")]
    public async Task<IActionResult> Track(string warrantyNumber, [FromQuery] string phoneNumber)
    {
        var warranty = await _repo.GetByWarrantyNumberAsync(warrantyNumber);
        if (warranty == null) return NotFound(new { error = "Warranty not found" });

        // Basic security: match phone number against client phone
        if (warranty.Client?.Phone != phoneNumber) 
            return Unauthorized(new { error = "Invalid credentials" });

        return Ok(_mapper.Map<WarrantyResponse>(warranty));
    }
}
