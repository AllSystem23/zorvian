using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.DTOs.Report;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class FleetExpenseService
{
    private readonly IFleetExpenseRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly IAutoAccountingService _autoAccounting;

    public FleetExpenseService(
        IFleetExpenseRepository repo,
        ITenantContext tenant,
        IMapper mapper,
        IAutoAccountingService autoAccounting)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
        _autoAccounting = autoAccounting;
    }

    public async Task<List<FleetExpenseResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        var items = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<FleetExpenseResponse>>(items);
    }

    public async Task<FleetExpenseResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<FleetExpenseResponse>(item);
    }

    public async Task<FleetExpenseResponse> CreateAsync(CreateFleetExpenseRequest request)
    {
        var entity = _mapper.Map<FleetExpense>(request);
        entity.AmountBaseCurrency = request.Amount * request.ExchangeRate;
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        // Accounting entry is generated on approval (ApproveAsync), not on create.
        // This ensures proper approval workflow: create → approve → accounting entry.

        return _mapper.Map<FleetExpenseResponse>(entity);
    }

    /// <summary>
    /// Approve a fleet expense and generate its accounting entry (single entry point).
    /// </summary>
    public async Task<FleetExpenseResponse?> ApproveAsync(Guid id, Guid? accountId = null)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        if (entity.Approved) return _mapper.Map<FleetExpenseResponse>(entity);

        entity.Approved = true;

        // Use provided accountId, or the one already on the expense, or null for auto-fallback
        var effectiveAccountId = accountId ?? entity.AccountId;

        if (Guid.TryParse(_tenant.TenantId, out var companyId))
        {
            try
            {
                await _autoAccounting.GenerateFleetExpenseEntryAsync(
                    entity.Id, entity.AmountBaseCurrency, entity.Description,
                    effectiveAccountId, companyId, null);
                entity.AccountId = effectiveAccountId;
            }
            catch (Exception)
            {
                // Accounting entry generation failed but expense is still approved
                // Log in production via ILogger — best-effort for now
            }
        }

        await _repo.SaveChangesAsync();
        return _mapper.Map<FleetExpenseResponse>(entity);
    }

    /// <summary>
    /// Approve multiple fleet expenses in batch with auto-classification.
    /// </summary>
    public async Task<BatchApproveResult> ApproveBatchAsync(List<Guid> ids)
    {
        var approved = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var id in ids)
        {
            var result = await ApproveAsync(id);
            if (result is null)
                failed++;
            else if (result.Approved)
                approved++;
            else
                skipped++;
        }

        return new BatchApproveResult(ids.Count, approved, skipped, failed);
    }

    public async Task<FleetExpenseResponse?> UpdateAsync(Guid id, UpdateFleetExpenseRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        if (request.Amount != null && request.ExchangeRate != null)
            entity.AmountBaseCurrency = request.Amount.Value * request.ExchangeRate.Value;
        await _repo.SaveChangesAsync();
        return _mapper.Map<FleetExpenseResponse>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;
        await _repo.DeleteAsync(entity);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ── Export ──

    public async Task<ReportResult> GetExportResultAsync(string? status = null, DateTime? startDate = null, DateTime? endDate = null, Guid? vehicleId = null)
    {
        var items = await GetAllAsync();

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            items = status.ToLower() switch
            {
                "approved" => items.Where(i => i.Approved).ToList(),
                "pending" => items.Where(i => !i.Approved).ToList(),
                _ => items,
            };
        }
        if (startDate.HasValue)
            items = items.Where(i => i.ExpenseDate >= startDate.Value).ToList();
        if (endDate.HasValue)
            items = items.Where(i => i.ExpenseDate <= endDate.Value).ToList();
        if (vehicleId.HasValue)
            items = items.Where(i => i.VehicleId == vehicleId.Value).ToList();

        var columns = new List<string> { "Fecha", "Descripción", "Categoría", "Subcategoría", "Vehículo", "Conductor", "Monto", "Moneda", "Método", "Estado", "Reembolsable" };
        var rows = items.Select(e => new Dictionary<string, object?>
        {
            ["Fecha"] = e.ExpenseDate,
            ["Descripción"] = e.Description,
            ["Categoría"] = e.CategoryName,
            ["Subcategoría"] = e.SubcategoryName ?? "-",
            ["Vehículo"] = e.VehiclePlate ?? "-",
            ["Conductor"] = e.DriverName ?? "-",
            ["Monto"] = e.Amount,
            ["Moneda"] = e.Currency,
            ["Método"] = e.PaymentMethod,
            ["Estado"] = e.Approved ? "Aprobado" : "Pendiente",
            ["Reembolsable"] = e.Reimbursable ? "Sí" : "No",
        }).ToList();

        return new ReportResult(columns, rows, items.Count);
    }

    // ── Stats for Notifications ──

    public async Task<int> GetPendingCountAsync()
    {
        var items = await GetAllAsync();
        return items.Count(i => !i.Approved);
    }

    public async Task<decimal> GetPendingAmountAsync()
    {
        var items = await GetAllAsync();
        return items.Where(i => !i.Approved).Sum(i => i.Amount);
    }
}

public sealed record BatchApproveResult(int Total, int Approved, int Skipped, int Failed);
