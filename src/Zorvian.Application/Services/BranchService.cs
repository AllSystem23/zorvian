using AutoMapper;
using Zorvian.Application.DTOs.Branch;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class BranchService
{
    private readonly IBranchRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public BranchService(IBranchRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<BranchResponse>> GetAllAsync()
    {
        var companyId = Guid.Parse(_tenant.TenantId);
        var branches = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<BranchResponse>>(branches);
    }

    public async Task<BranchResponse?> GetByIdAsync(Guid id)
    {
        var branch = await _repo.GetByIdAsync(id);
        return branch is null ? null : _mapper.Map<BranchResponse>(branch);
    }

    public async Task<BranchResponse> CreateAsync(CreateBranchRequest request)
    {
        var branch = _mapper.Map<Branch>(request);
        branch.CompanyId = Guid.Parse(_tenant.TenantId);

        await _repo.AddAsync(branch);
        await _repo.SaveChangesAsync();

        return _mapper.Map<BranchResponse>(branch);
    }

    public async Task<BranchResponse?> UpdateAsync(Guid id, UpdateBranchRequest request)
    {
        var branch = await _repo.GetByIdAsync(id);
        if (branch is null) return null;

        _mapper.Map(request, branch);
        await _repo.SaveChangesAsync();

        return _mapper.Map<BranchResponse>(branch);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var branch = await _repo.GetByIdAsync(id);
        if (branch is null) return false;

        await _repo.DeleteAsync(branch);
        await _repo.SaveChangesAsync();
        return true;
    }
}
