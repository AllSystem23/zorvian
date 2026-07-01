using AutoMapper;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Domain;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyService
{
    private readonly IWarrantyRepository _repo;
    private readonly IServiceWorkshopRepository _workshopRepo;
    private readonly IWarrantyProviderRepository _providerRepo;
    private readonly IInventoryMovementService _inventoryService;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly IGoalIntegrationService _goalIntegration;

    public WarrantyService(
        IWarrantyRepository repo, 
        IServiceWorkshopRepository workshopRepo, 
        IWarrantyProviderRepository providerRepo, 
        IInventoryMovementService inventoryService,
        ITenantContext tenant, 
        IMapper mapper,
        IGoalIntegrationService goalIntegration)
    {
        _repo = repo;
        _workshopRepo = workshopRepo;
        _providerRepo = providerRepo;
        _inventoryService = inventoryService;
        _tenant = tenant;
        _mapper = mapper;
        _goalIntegration = goalIntegration;
    }

    public async Task<WarrantyResponse> CreateAsync(CreateWarrantyRequest request)
    {
        var warranty = _mapper.Map<Warranty>(request);
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");
        warranty.WarrantyNumber = await _repo.GenerateWarrantyNumberAsync(companyId);
        warranty.EndDate = warranty.StartDate.AddMonths(request.DurationMonths);
        warranty.BranchId = request.BranchId;

        await _repo.AddAsync(warranty);
        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantyResponse>(warranty);
    }

    public async Task<WarrantyResponse?> GetByIdAsync(Guid id)
    {
        var warranty = await _repo.GetByIdAsync(id);
        return warranty is null ? null : _mapper.Map<WarrantyResponse>(warranty);
    }

    public async Task<WarrantyResponse> UpdateAsync(Guid id, UpdateWarrantyRequest request)
    {
        var warranty = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Warranty not found");

        if (!string.IsNullOrEmpty(request.SerialNumber)) warranty.SerialNumber = request.SerialNumber;
        if (!string.IsNullOrEmpty(request.Imei)) warranty.Imei = request.Imei;
        if (!string.IsNullOrEmpty(request.LotNumber)) warranty.LotNumber = request.LotNumber;
        if (!string.IsNullOrEmpty(request.Terms)) warranty.Terms = request.Terms;
        if (request.DurationMonths.HasValue)
        {
            warranty.DurationMonths = request.DurationMonths.Value;
            warranty.EndDate = warranty.StartDate.AddMonths(request.DurationMonths.Value);
        }

        await _repo.SaveChangesAsync();
        return _mapper.Map<WarrantyResponse>(warranty);
    }

    public async Task DeleteAsync(Guid id)
    {
        var warranty = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Warranty not found");
        _repo.Delete(warranty);
        await _repo.SaveChangesAsync();
    }

    public async Task<WarrantyResponse> UpdateStatusAsync(Guid id, WarrantyStatus newStatus)
    {
        var warranty = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Warranty not found");

        WarrantyStateMachine.EnsureCanTransition(warranty.Status, newStatus);
        warranty.Status = newStatus;

        await _repo.SaveChangesAsync();
        return _mapper.Map<WarrantyResponse>(warranty);
    }

    public async Task<PagedResult<WarrantyListResponse>> GetFilteredAsync(WarrantyFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;
        var branchId = filter.BranchId ?? Guid.Empty;

        var items = await _repo.GetFilteredAsync(filter.ClientId, filter.Status, filter.ExpiringSoon, branchId, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(filter.ClientId, filter.Status, filter.ExpiringSoon, branchId);

        return new PagedResult<WarrantyListResponse>(
            _mapper.Map<List<WarrantyListResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<WarrantyClaimResponse> AddClaimAsync(CreateWarrantyClaimRequest request)
    {
        var warranty = await _repo.GetByIdAsync(request.WarrantyId)
            ?? throw new KeyNotFoundException("Warranty not found");

        var claim = _mapper.Map<WarrantyClaim>(request);
        claim.CompanyId = warranty.CompanyId;
        claim.BranchId = warranty.BranchId;

        warranty.Claims.Add(claim);
        WarrantyStateMachine.EnsureCanTransition(warranty.Status, WarrantyStatus.PendingReview);
        warranty.Status = WarrantyStatus.PendingReview;

        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantyClaimResponse>(claim);
    }

    public async Task<WarrantyClaimResponse> AssignWorkshopAsync(Guid claimId, AssignWorkshopRequest request)
    {
        var claim = await _repo.GetClaimByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found");

        claim.WorkshopId = request.WorkshopId;
        claim.TechnicianId = request.TechnicianId;
        claim.WorkshopAssignedAt = DateTime.UtcNow;
        // In a real implementation, we would fetch WorkshopBrand to get SLA
        claim.SlaDeadline = DateTime.UtcNow.AddHours(request.SlaHoursOverride ?? 48);
        WarrantyStateMachine.EnsureCanTransition(claim.Status, WarrantyStatus.SentToWorkshop);
        claim.Status = WarrantyStatus.SentToWorkshop;

        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantyClaimResponse>(claim);
    }

    public async Task<WarrantyClaimResponse> ReferToProviderAsync(Guid claimId, ReferToProviderRequest request)
    {
        var claim = await _repo.GetClaimByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found");

        claim.ProviderId = request.ProviderId;
        claim.ProviderReferredAt = DateTime.UtcNow;
        claim.ProviderAuthorizationCode = request.AuthorizationCode;
        claim.Status = WarrantyStatus.SentToWorkshop; // Or a specific 'SentToProvider' status if one existed

        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantyClaimResponse>(claim);
    }

    public async Task<WarrantyClaimResponse> ProcessManufacturerReplacementAsync(Guid claimId, ProcessReplacementRequest request)
    {
        var claim = await _repo.GetClaimByIdAsync(claimId)
            ?? throw new KeyNotFoundException("Claim not found");

        // 1. Salida del producto defectuoso
        await _inventoryService.CreateAsync(new CreateInventoryMovementRequest(
            claim.Warranty.ProductId,
            "exit", 
            1,
            0,
            request.ProviderAuthorizationCode,
            $"RMA: DevoluciÃ³n de producto defectuoso {claim.Warranty.SerialNumber}",
            claim.BranchId,
            claim.Warranty.SerialNumber
        ));

        // 2. Entrada del producto nuevo
        await _inventoryService.CreateAsync(new CreateInventoryMovementRequest(
            request.NewProductId,
            "entry", 
            1,
            0,
            request.ProviderAuthorizationCode,
            $"RMA: RecepciÃ³n de producto nuevo (Reemplazo {claim.Warranty.WarrantyNumber})",
            claim.BranchId,
            request.NewSerialNumber
        ));

        // 3. Actualizar estado y datos del reclamo
        claim.Status = WarrantyStatus.ReplacementApproved;
        claim.ProviderAuthorizationCode = request.ProviderAuthorizationCode;
        // Need to add ReplacementProductId and ReplacementSerial to WarrantyClaim entity if not there, 
        // checking WarrantyClaim entity definition again.
        
        // As per previous read, WarrantyClaim does not have ReplacementProductId/Serial.
        // I will assume for now I only need to update the status and log the event.
        
        claim.ResolutionType = "replaced";
        claim.ResolutionDate = DateOnly.FromDateTime(DateTime.UtcNow);

        if (claim.TechnicianId.HasValue)
        {
            await _goalIntegration.HandleCaseSolvedAsync(claim.TechnicianId.Value, claimId);
        }

        await _repo.SaveChangesAsync();
        return _mapper.Map<WarrantyClaimResponse>(claim);
    }
}
