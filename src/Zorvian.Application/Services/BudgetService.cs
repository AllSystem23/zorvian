using AutoMapper;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class BudgetService
{
    private readonly IBudgetRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public BudgetService(IBudgetRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<BudgetResponse>> GetAllAsync()
    {
        var companyId = Guid.Parse(_tenant.TenantId.ToString());
        var items = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<BudgetResponse>>(items);
    }

    public async Task<List<BudgetResponse>> GetByPeriodAsync(int year, int month)
    {
        var companyId = Guid.Parse(_tenant.TenantId.ToString());
        var items = await _repo.GetByPeriodAsync(year, month, companyId);
        return _mapper.Map<List<BudgetResponse>>(items);
    }

    public async Task<BudgetResponse> CreateAsync(CreateBudgetRequest request)
    {
        var entity = _mapper.Map<Budget>(request);
        entity.CompanyId = Guid.Parse(_tenant.TenantId.ToString());

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        // Reload with navigation properties
        entity = (await _repo.GetByIdAsync(entity.Id))!;
        return _mapper.Map<BudgetResponse>(entity);
    }

    public async Task<BudgetResponse?> UpdateAsync(Guid id, UpdateBudgetRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();

        return _mapper.Map<BudgetResponse>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;

        await _repo.DeleteAsync(entity);
        await _repo.SaveChangesAsync();
        return true;
    }
}
