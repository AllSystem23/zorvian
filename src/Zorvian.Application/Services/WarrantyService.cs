using AutoMapper;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyService
{
    private readonly IWarrantyRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public WarrantyService(IWarrantyRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<WarrantyResponse> CreateAsync(CreateWarrantyRequest request)
    {
        var warranty = _mapper.Map<Warranty>(request);
        warranty.WarrantyNumber = await _repo.GenerateWarrantyNumberAsync(Guid.Parse(_tenant.TenantId));
        warranty.EndDate = warranty.StartDate.AddMonths(request.DurationMonths);
        warranty.CompanyId = Guid.Parse(_tenant.TenantId);

        await _repo.AddAsync(warranty);
        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantyResponse>(warranty);
    }

    public async Task<WarrantyResponse?> GetByIdAsync(Guid id)
    {
        var warranty = await _repo.GetByIdAsync(id);
        return warranty is null ? null : _mapper.Map<WarrantyResponse>(warranty);
    }

    public async Task<PagedResult<WarrantyListResponse>> GetFilteredAsync(WarrantyFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _repo.GetFilteredAsync(filter.ClientId, filter.Status, filter.ExpiringSoon, Guid.Empty, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(filter.ClientId, filter.Status, filter.ExpiringSoon, Guid.Empty);

        return new PagedResult<WarrantyListResponse>(
            _mapper.Map<List<WarrantyListResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<WarrantyClaimResponse> AddClaimAsync(CreateWarrantyClaimRequest request)
    {
        var warranty = await _repo.GetByIdAsync(request.WarrantyId)
            ?? throw new InvalidOperationException("Warranty not found");

        var claim = _mapper.Map<WarrantyClaim>(request);
        claim.CompanyId = warranty.CompanyId;
        claim.BranchId = warranty.BranchId;

        warranty.Claims.Add(claim);
        warranty.Status = "claimed";

        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantyClaimResponse>(claim);
    }
}
