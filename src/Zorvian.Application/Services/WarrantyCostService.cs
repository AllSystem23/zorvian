using AutoMapper;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public class WarrantyCostService : IWarrantyCostService
{
    private readonly IWarrantyCostRepository _repo;
    private readonly IMapper _mapper;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ITenantContext _tenant;

    public WarrantyCostService(IWarrantyCostRepository repo, IMapper mapper, IAutoAccountingService autoAccounting, ITenantContext tenant)
    {
        _repo = repo;
        _mapper = mapper;
        _autoAccounting = autoAccounting;
        _tenant = tenant;
    }

    public async Task<List<WarrantyCostResponse>> GetByWarrantyIdAsync(Guid warrantyId)
    {
        var costs = await _repo.GetByWarrantyIdAsync(warrantyId);
        return _mapper.Map<List<WarrantyCostResponse>>(costs);
    }

    public async Task<List<WarrantyCostResponse>> GetByClaimIdAsync(Guid claimId)
    {
        var costs = await _repo.GetByClaimIdAsync(claimId);
        return _mapper.Map<List<WarrantyCostResponse>>(costs);
    }

    public async Task<WarrantyCostResponse?> GetByIdAsync(Guid id)
    {
        var cost = await _repo.GetByIdAsync(id);
        return cost is null ? null : _mapper.Map<WarrantyCostResponse>(cost);
    }

    public async Task<WarrantyCostResponse> CreateAsync(CreateWarrantyCostRequest request)
    {
        var cost = _mapper.Map<WarrantyCost>(request);
        cost.CompanyId = Guid.TryParse(_tenant.TenantId, out var companyId) ? companyId : throw new InvalidOperationException("Invalid tenant");
        await _repo.AddAsync(cost);
        await _repo.SaveChangesAsync();

        if (cost.IsBilled)
        {
            var entryId = await _autoAccounting.GenerateWarrantyCostEntryAsync(
                cost.Id, cost.CostCategory, cost.Quantity * cost.UnitCost,
                cost.PaidBy, cost.PaidByPartyId, cost.WarrantyId,
                cost.CompanyId, cost.BranchId);
            cost.AccountingEntryId = entryId;
            await _repo.SaveChangesAsync();
        }

        return _mapper.Map<WarrantyCostResponse>(cost);
    }

    public async Task<WarrantyCostResponse?> UpdateAsync(Guid id, UpdateWarrantyCostRequest request)
    {
        var cost = await _repo.GetByIdAsync(id);
        if (cost is null) return null;

        var wasUnbilled = !cost.IsBilled;
        var willBeBilled = request.IsBilled == true;

        cost.CompanyId = Guid.TryParse(_tenant.TenantId, out var companyId) ? companyId : throw new InvalidOperationException("Invalid tenant");

        _mapper.Map(request, cost);

        if (wasUnbilled && willBeBilled)
        {
            var entryId = await _autoAccounting.GenerateWarrantyCostEntryAsync(
                cost.Id, cost.CostCategory, cost.Quantity * cost.UnitCost,
                cost.PaidBy, cost.PaidByPartyId, cost.WarrantyId,
                cost.CompanyId, cost.BranchId);
            cost.AccountingEntryId = entryId;
        }

        await _repo.SaveChangesAsync();
        return _mapper.Map<WarrantyCostResponse>(cost);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var cost = await _repo.GetByIdAsync(id);
        if (cost is null) return false;

        await _repo.DeleteAsync(cost);
        await _repo.SaveChangesAsync();
        return true;
    }
}
