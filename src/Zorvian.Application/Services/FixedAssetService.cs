using AutoMapper;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.FixedAssets;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.DepreciationCalculators;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class FixedAssetService
{
    private readonly IFixedAssetRepository _assetRepo;
    private readonly IFixedAssetCategoryRepository _catRepo;
    private readonly IDepreciationEntryRepository _deprRepo;
    private readonly IAssetRevaluationRepository _revalRepo;
    private readonly IAssetDisposalRepository _disposalRepo;
    private readonly IAssetMaintenanceRepository _maintRepo;
    private readonly ILocationRepository _locRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly DepreciationCalculatorFactory _calcFactory;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public FixedAssetService(
        IFixedAssetRepository assetRepo,
        IFixedAssetCategoryRepository catRepo,
        IDepreciationEntryRepository deprRepo,
        IAssetRevaluationRepository revalRepo,
        IAssetDisposalRepository disposalRepo,
        IAssetMaintenanceRepository maintRepo,
        ILocationRepository locRepo,
        IDepartmentRepository deptRepo,
        ISupplierRepository supplierRepo,
        IAutoAccountingService autoAccounting,
        DepreciationCalculatorFactory calcFactory,
        ITenantContext tenant,
        IMapper mapper)
    {
        _assetRepo = assetRepo;
        _catRepo = catRepo;
        _deprRepo = deprRepo;
        _revalRepo = revalRepo;
        _disposalRepo = disposalRepo;
        _maintRepo = maintRepo;
        _locRepo = locRepo;
        _deptRepo = deptRepo;
        _supplierRepo = supplierRepo;
        _autoAccounting = autoAccounting;
        _calcFactory = calcFactory;
        _tenant = tenant;
        _mapper = mapper;
    }

    private Guid CompanyId => _tenant.RequireCompanyId();

    public async Task<FixedAssetResponse> CreateAsync(CreateFixedAssetRequest request)
    {
        var companyId = CompanyId;

        var asset = _mapper.Map<FixedAsset>(request);
        asset.Code = await _assetRepo.GenerateCodeAsync(companyId);
        asset.CompanyId = companyId;
        asset.Status = "active";
        asset.IsActive = true;

        await _assetRepo.AddAsync(asset);
        await _assetRepo.SaveChangesAsync();

        await _autoAccounting.GenerateFixedAssetAcquisitionEntryAsync(
            asset.Id, request.AcquisitionCost, companyId, request.BranchId);

        return await GetByIdAsync(asset.Id) ?? throw new InvalidOperationException("Failed to create asset");
    }

    public async Task<FixedAssetResponse> UpdateAsync(Guid id, UpdateFixedAssetRequest request)
    {
        var asset = await _assetRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Asset not found");

        _mapper.Map(request, asset);
        await _assetRepo.UpdateAsync(asset);
        await _assetRepo.SaveChangesAsync();

        return await GetByIdAsync(id) ?? throw new InvalidOperationException("Failed to update asset");
    }

    public async Task<FixedAssetResponse?> GetByIdAsync(Guid id)
    {
        var asset = await _assetRepo.GetByIdAsync(id);
        if (asset is null) return null;

        return MapToResponse(asset);
    }

    public async Task<PagedResult<FixedAssetListResponse>> GetFilteredAsync(FixedAssetFilterRequest filter)
    {
        var companyId = CompanyId;
        var items = await _assetRepo.GetFilteredAsync(
            filter.CategoryId, filter.Status, filter.LocationId, filter.DepartmentId,
            filter.Search, filter.FromDate, filter.ToDate,
            companyId, filter.Page ?? 1, filter.PageSize ?? 20);
        var total = await _assetRepo.GetFilteredCountAsync(
            filter.CategoryId, filter.Status, filter.LocationId, filter.DepartmentId,
            filter.Search, filter.FromDate, filter.ToDate, companyId);

        return new PagedResult<FixedAssetListResponse>(
            _mapper.Map<List<FixedAssetListResponse>>(items), total, filter.Page ?? 1, filter.PageSize ?? 20);
    }

    public async Task<DepreciationEntryResponse> RunDepreciationAsync(Guid id, RunDepreciationRequest request)
    {
        var asset = await _assetRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Asset not found");

        if (asset.Status != "active")
            throw new InvalidOperationException($"Cannot depreciate asset with status '{asset.Status}'");

        var calc = _calcFactory.GetCalculator(asset.DepreciationMethod);

        var lastEntry = await _deprRepo.GetLastByAssetIdAsync(asset.Id);
        var currentPeriod = lastEntry != null
            ? (int)((request.PeriodDate.Year - asset.AcquisitionDate.Year) * 12 +
                     request.PeriodDate.Month - asset.AcquisitionDate.Month) + 1
            : 1;

        var accumulatedDep = lastEntry?.AccumulatedDepreciation ?? 0;
        var maxDepreciable = asset.AcquisitionCost - asset.ResidualValue;
        var remaining = maxDepreciable - accumulatedDep;

        if (remaining <= 0)
            throw new InvalidOperationException("Asset is fully depreciated");

        var amount = calc.Calculate(
            asset.AcquisitionCost, asset.ResidualValue, asset.UsefulLifeYears,
            currentPeriod, asset.TotalUnits, asset.UnitsProduced, accumulatedDep);

        amount = Math.Min(amount, remaining);

        var newAccumulated = accumulatedDep + amount;
        var netBookValue = asset.AcquisitionCost - newAccumulated;

        var entryId = await _autoAccounting.GenerateDepreciationEntryAsync(
            asset.Id, amount, CompanyId, asset.BranchId);

        var deprEntry = new DepreciationEntry
        {
            FixedAssetId = asset.Id,
            PeriodDate = request.PeriodDate,
            Amount = amount,
            AccumulatedDepreciation = newAccumulated,
            NetBookValue = netBookValue,
            AccountingEntryId = entryId,
            CompanyId = CompanyId,
        };

        await _deprRepo.AddAsync(deprEntry);
        await _deprRepo.SaveChangesAsync();

        if (netBookValue <= asset.ResidualValue || newAccumulated >= maxDepreciable)
        {
            asset.Status = "depreciated";
            await _assetRepo.UpdateAsync(asset);
            await _assetRepo.SaveChangesAsync();
        }

        return _mapper.Map<DepreciationEntryResponse>(deprEntry);
    }

    public async Task<int> RunBulkDepreciationAsync(DateTime periodDate)
    {
        var companyId = CompanyId;
        var assets = await _assetRepo.GetActiveForDepreciationAsync(periodDate, companyId);
        var count = 0;

        foreach (var asset in assets)
        {
            try
            {
                await RunDepreciationAsync(asset.Id, new RunDepreciationRequest(periodDate));
                count++;
            }
            catch
            {
                // Skip assets that error
            }
        }

        return count;
    }

    public async Task<AssetRevaluationResponse> RevalueAsync(Guid id, RevalueAssetRequest request)
    {
        var asset = await _assetRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Asset not found");

        var lastDepr = await _deprRepo.GetLastByAssetIdAsync(asset.Id);
        var accumulatedDep = lastDepr?.AccumulatedDepreciation ?? 0;
        var previousValue = asset.AcquisitionCost;

        var revaluation = new AssetRevaluation
        {
            FixedAssetId = asset.Id,
            RevaluationDate = DateTime.UtcNow,
            PreviousValue = previousValue,
            NewValue = request.NewValue,
            PreviousAccumulatedDepreciation = accumulatedDep,
            Reason = request.Reason,
            ApprovedBy = request.ApprovedBy,
            CompanyId = CompanyId,
        };

        asset.AcquisitionCost = request.NewValue;

        await _revalRepo.AddAsync(revaluation);
        await _assetRepo.UpdateAsync(asset);
        await _assetRepo.SaveChangesAsync();

        var entryId = await _autoAccounting.GenerateRevaluationEntryAsync(
            asset.Id, previousValue, request.NewValue, accumulatedDep, CompanyId, asset.BranchId);

        revaluation.AccountingEntryId = entryId;
        await _revalRepo.SaveChangesAsync();

        return _mapper.Map<AssetRevaluationResponse>(revaluation);
    }

    public async Task<AssetDisposalResponse> DisposeAsync(Guid id, DisposeAssetRequest request)
    {
        var asset = await _assetRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Asset not found");

        if (asset.Status == "disposed")
            throw new InvalidOperationException("Asset is already disposed");

        var lastDepr = await _deprRepo.GetLastByAssetIdAsync(asset.Id);
        var accumulatedDep = lastDepr?.AccumulatedDepreciation ?? 0;
        var netBookValue = asset.AcquisitionCost - accumulatedDep;
        var saleAmount = request.SaleAmount ?? 0;
        var gainOrLoss = saleAmount - netBookValue;

        var disposal = new AssetDisposal
        {
            FixedAssetId = asset.Id,
            DisposalDate = DateTime.UtcNow,
            DisposalType = request.DisposalType,
            SaleAmount = request.SaleAmount,
            NetBookValueAtDisposal = netBookValue,
            GainOrLoss = gainOrLoss,
            Reason = request.Reason,
            ApprovedBy = request.ApprovedBy,
            CompanyId = CompanyId,
        };

        asset.Status = "disposed";
        asset.IsActive = false;

        await _disposalRepo.AddAsync(disposal);
        await _assetRepo.UpdateAsync(asset);
        await _assetRepo.SaveChangesAsync();

        var entryId = await _autoAccounting.GenerateDisposalEntryAsync(
            asset.Id, asset.AcquisitionCost, accumulatedDep, saleAmount,
            gainOrLoss, request.DisposalType, CompanyId, asset.BranchId);

        disposal.AccountingEntryId = entryId;
        await _disposalRepo.SaveChangesAsync();

        return _mapper.Map<AssetDisposalResponse>(disposal);
    }

    public async Task<AssetMaintenanceResponse> AddMaintenanceAsync(Guid id, AddMaintenanceRequest request)
    {
        var asset = await _assetRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Asset not found");

        var maintenance = new AssetMaintenance
        {
            FixedAssetId = asset.Id,
            MaintenanceDate = request.MaintenanceDate,
            MaintenanceType = request.MaintenanceType,
            Description = request.Description,
            Cost = request.Cost,
            Provider = request.Provider,
            NextMaintenanceDate = request.NextMaintenanceDate,
            EstimatedDurationHours = request.EstimatedDurationHours,
            Status = request.Status ?? "completed",
            CompanyId = CompanyId,
        };

        await _maintRepo.AddAsync(maintenance);
        await _maintRepo.SaveChangesAsync();

        return _mapper.Map<AssetMaintenanceResponse>(maintenance);
    }

    public async Task<List<DepreciationScheduleItem>> GetDepreciationScheduleAsync(Guid id)
    {
        var asset = await _assetRepo.GetByIdAsync(id);
        if (asset is null) return [];

        var calc = _calcFactory.GetCalculator(asset.DepreciationMethod);
        var lastEntry = await _deprRepo.GetLastByAssetIdAsync(asset.Id);
        var startAccumulated = lastEntry?.AccumulatedDepreciation ?? 0;
        var startPeriod = lastEntry != null
            ? (int)((lastEntry.PeriodDate.Year - asset.AcquisitionDate.Year) * 12 +
                     lastEntry.PeriodDate.Month - asset.AcquisitionDate.Month) + 1
            : 0;
        var maxDepreciable = asset.AcquisitionCost - asset.ResidualValue;
        var schedule = new List<DepreciationScheduleItem>();
        var accumulated = startAccumulated;

        var monthsToProject = asset.UsefulLifeYears * 12;
        var startMonth = lastEntry != null
            ? lastEntry.PeriodDate.AddMonths(1)
            : asset.AcquisitionDate.AddMonths(1);

        for (int i = 0; i < monthsToProject && accumulated < maxDepreciable; i++)
        {
            var currentPeriod = startPeriod + i + 1;
            var amount = calc.Calculate(
                asset.AcquisitionCost, asset.ResidualValue, asset.UsefulLifeYears,
                currentPeriod, asset.TotalUnits, asset.UnitsProduced, accumulated);

            amount = Math.Min(amount, maxDepreciable - accumulated);
            if (amount <= 0) break;

            accumulated += amount;
            var projectionDate = startMonth.AddMonths(i);

            schedule.Add(new DepreciationScheduleItem(
                projectionDate.Year, projectionDate.Month, amount,
                accumulated, asset.AcquisitionCost - accumulated));
        }

        return schedule;
    }

    public async Task<FixedAssetSummaryResponse> GetSummaryAsync()
    {
        var companyId = CompanyId;
        var allAssets = await _assetRepo.GetFilteredAsync(null, null, null, null, null, null, null, companyId, 1, int.MaxValue);

        var totalCost = allAssets.Sum(a => a.AcquisitionCost);

        var accumulatedDep = 0m;
        var netBookValues = new Dictionary<Guid, decimal>();
        foreach (var asset in allAssets)
        {
            var lastDepr = await _deprRepo.GetLastByAssetIdAsync(asset.Id);
            var acc = lastDepr?.AccumulatedDepreciation ?? 0;
            accumulatedDep += acc;
            netBookValues[asset.Id] = asset.AcquisitionCost - acc;
        }

        var totalNetBook = netBookValues.Values.Sum();

        var byCategory = allAssets
            .GroupBy(a => a.Category?.Name ?? "Sin categoría")
            .Select(g => new CategorySummaryItem(
                g.Key, g.Count(),
                g.Sum(a => a.AcquisitionCost),
                g.Sum(a => netBookValues.GetValueOrDefault(a.Id, a.AcquisitionCost))))
            .ToList();

        var byStatus = allAssets
            .GroupBy(a => a.Status)
            .Select(g => new StatusSummaryItem(
                g.Key, g.Count(),
                g.Sum(a => netBookValues.GetValueOrDefault(a.Id, a.AcquisitionCost))))
            .ToList();

        return new FixedAssetSummaryResponse(
            allAssets.Count,
            allAssets.Count(a => a.Status == "active"),
            allAssets.Count(a => a.Status == "disposed"),
            totalCost, accumulatedDep, totalNetBook,
            byCategory, byStatus);
    }

    public async Task<List<AssetMaintenanceResponse>> GetUpcomingMaintenanceAsync(int days = 30)
    {
        var companyId = CompanyId;
        var items = await _maintRepo.GetUpcomingAsync(companyId, days);
        return _mapper.Map<List<AssetMaintenanceResponse>>(items);
    }

    private FixedAssetResponse MapToResponse(FixedAsset asset)
    {
        var lastDepr = asset.DepreciationEntries?.MaxBy(e => e.PeriodDate);
        var accumulatedDep = lastDepr?.AccumulatedDepreciation ?? 0;

        return new FixedAssetResponse(
            asset.Id, asset.Code, asset.Name, asset.Description,
            asset.CategoryId, asset.Category?.Name,
            asset.SerialNumber, asset.Barcode, asset.Brand, asset.Model,
            asset.AcquisitionDate, asset.AcquisitionCost,
            asset.SupplierId, asset.Supplier?.Name,
            asset.InvoiceReference, asset.PurchaseId,
            asset.UsefulLifeYears, asset.ResidualValue, asset.DepreciationMethod,
            asset.TotalUnits, asset.UnitsProduced,
            accumulatedDep, asset.AcquisitionCost - accumulatedDep,
            asset.LocationId, asset.Location?.Name,
            asset.DepartmentId, asset.Department?.Name,
            asset.AssignedTo, asset.Status, asset.ImageUrl, asset.CreatedAt,
            _mapper.Map<List<DepreciationEntryResponse>>(asset.DepreciationEntries ?? []),
            _mapper.Map<List<AssetRevaluationResponse>>(asset.Revaluations ?? []),
            _mapper.Map<List<AssetMaintenanceResponse>>(asset.MaintenanceRecords ?? []),
            asset.Disposal != null ? _mapper.Map<AssetDisposalResponse>(asset.Disposal) : null
        );
    }
}
