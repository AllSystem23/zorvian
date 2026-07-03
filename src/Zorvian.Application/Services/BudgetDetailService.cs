using AutoMapper;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class BudgetDetailService
{
    private readonly IBudgetDetailRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public BudgetDetailService(IBudgetDetailRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    private Guid RequireCompanyId()
    {
        if (_tenant.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Seleccione una empresa antes de continuar.");
        return _tenant.TenantId.Value;
    }

    public async Task<List<BudgetDetailResponse>> GetByBudgetIdAsync(Guid budgetId)
    {
        var items = await _repo.GetByBudgetIdAsync(budgetId);
        return items.Select(i => new BudgetDetailResponse(
            i.Id, i.BudgetId, i.AccountId, i.Account?.Name ?? "",
            i.Account?.Code ?? "", i.CostCenterId, i.CostCenter?.Name,
            i.BudgetedAmount, i.Description, i.Month, i.Year
        )).ToList();
    }

    public async Task<List<BudgetDetailResponse>> GetByPeriodAsync(int year, int month)
    {
        var companyId = RequireCompanyId();
        var items = await _repo.GetByPeriodAsync(year, month, companyId);
        return items.Select(i => new BudgetDetailResponse(
            i.Id, i.BudgetId, i.AccountId, i.Account?.Name ?? "",
            i.Account?.Code ?? "", i.CostCenterId, i.CostCenter?.Name,
            i.BudgetedAmount, i.Description, i.Month, i.Year
        )).ToList();
    }

    public async Task<BudgetDetailResponse> CreateAsync(CreateBudgetDetailRequest request)
    {
        var companyId = RequireCompanyId();
        var entity = new BudgetDetail
        {
            BudgetId = request.BudgetId,
            AccountId = request.AccountId,
            CostCenterId = request.CostCenterId,
            BudgetedAmount = request.BudgetedAmount,
            Description = request.Description,
            Month = request.Month,
            Year = request.Year,
            TenantId = _tenant.TenantId.ToString(),
            CompanyId = companyId,
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        return (await _repo.GetByIdAsync(entity.Id)) is { } saved
            ? new BudgetDetailResponse(saved.Id, saved.BudgetId, saved.AccountId,
                saved.Account?.Name ?? "", saved.Account?.Code ?? "",
                saved.CostCenterId, saved.CostCenter?.Name,
                saved.BudgetedAmount, saved.Description, saved.Month, saved.Year)
            : throw new InvalidOperationException("Error al crear el detalle presupuestario.");
    }

    public async Task<BudgetDetailResponse?> UpdateAsync(Guid id, UpdateBudgetDetailRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        if (request.BudgetedAmount.HasValue) entity.BudgetedAmount = request.BudgetedAmount.Value;
        if (request.Description is not null) entity.Description = request.Description;

        await _repo.SaveChangesAsync();

        return new BudgetDetailResponse(entity.Id, entity.BudgetId, entity.AccountId,
            entity.Account?.Name ?? "", entity.Account?.Code ?? "",
            entity.CostCenterId, entity.CostCenter?.Name,
            entity.BudgetedAmount, entity.Description, entity.Month, entity.Year);
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

public sealed class BudgetTrackingService
{
    private readonly IBudgetTrackingRepository _repo;
    private readonly IBudgetDetailRepository _detailRepo;
    private readonly ITenantContext _tenant;

    public BudgetTrackingService(IBudgetTrackingRepository repo, IBudgetDetailRepository detailRepo, ITenantContext tenant)
    {
        _repo = repo;
        _detailRepo = detailRepo;
        _tenant = tenant;
    }

    public async Task<PagedResult<BudgetTrackingResponse>> GetFilteredAsync(BudgetTrackingFilterRequest filter)
    {
        var companyId = _tenant.TenantId.Value == Guid.Empty ? Guid.Empty : _tenant.TenantId.Value;
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _repo.GetFilteredAsync(filter.BudgetDetailId, filter.Year, filter.Month, companyId, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(filter.BudgetDetailId, filter.Year, filter.Month, companyId);

        var mapped = items.Select(i => new BudgetTrackingResponse(
            i.Id, i.BudgetDetailId, i.AccountId,
            i.BudgetDetail?.Account?.Name ?? "",
            i.BudgetDetail?.Account?.Code ?? "",
            i.BudgetedAmount, i.ActualAmount, i.Variance, i.VariancePercentage,
            i.Month, i.Year, i.SourceReference, i.Notes
        )).ToList();

        return new PagedResult<BudgetTrackingResponse>(mapped, total, page, pageSize);
    }

    public async Task<BudgetTrackingResponse> CreateAsync(CreateBudgetTrackingRequest request)
    {
        var companyId = _tenant.TenantId.Value == Guid.Empty ? Guid.Empty : _tenant.TenantId.Value;

        // Resolve month/year from the BudgetDetail
        var budgetDetail = await _detailRepo.GetByIdAsync(request.BudgetDetailId);
        var month = budgetDetail?.Month ?? DateTime.UtcNow.Month;
        var year = budgetDetail?.Year ?? DateTime.UtcNow.Year;

        var entity = new BudgetTracking
        {
            BudgetDetailId = request.BudgetDetailId,
            ActualAmount = request.ActualAmount,
            SourceReference = request.SourceReference,
            Notes = request.Notes,
            TrackedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            Month = month,
            Year = year,
            TenantId = _tenant.TenantId.ToString(),
            CompanyId = companyId,
        };

        var detail = await _repo.GetByDetailAndPeriodAsync(request.BudgetDetailId, month, year);
        if (detail is not null)
        {
            detail.ActualAmount += request.ActualAmount;
            await _repo.SaveChangesAsync();
            return MapToResponse(detail);
        }

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        return MapToResponse(entity);
    }

    private static BudgetTrackingResponse MapToResponse(BudgetTracking t) => new(
        t.Id, t.BudgetDetailId, t.AccountId,
        t.BudgetDetail?.Account?.Name ?? "",
        t.BudgetDetail?.Account?.Code ?? "",
        t.BudgetedAmount, t.ActualAmount, t.Variance, t.VariancePercentage,
        t.Month, t.Year, t.SourceReference, t.Notes
    );
}
