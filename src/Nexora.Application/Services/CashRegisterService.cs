using AutoMapper;
using Nexora.Application.DTOs.CashRegister;
using Nexora.Application.DTOs.Common;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Application.Services;

public sealed class CashRegisterService
{
    private readonly ICashRegisterRepository _registerRepo;
    private readonly ICashMovementRepository _movementRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public CashRegisterService(
        ICashRegisterRepository registerRepo,
        ICashMovementRepository movementRepo,
        ITenantContext tenant,
        IMapper mapper)
    {
        _registerRepo = registerRepo;
        _movementRepo = movementRepo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<CashRegisterResponse> OpenAsync(OpenCashRegisterRequest request)
    {
        var existingOpen = await _registerRepo.GetOpenByBranchAsync(request.BranchId);
        if (existingOpen is not null)
            throw new InvalidOperationException("Already an open cash register in this branch");

        var register = _mapper.Map<CashRegister>(request);
        register.EmployeeId = _tenant.CurrentEmployeeId;
        register.CompanyId = Guid.Parse(_tenant.TenantId);

        await _registerRepo.AddAsync(register);
        await _registerRepo.SaveChangesAsync();

        return _mapper.Map<CashRegisterResponse>(register);
    }

    public async Task<CashRegisterResponse> CloseAsync(Guid id, CloseCashRegisterRequest request)
    {
        var register = await _registerRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Cash register not found");

        if (register.Status != "open")
            throw new InvalidOperationException("Cash register is already closed");

        register.ClosingBalance = request.ClosingBalance;
        register.ExpectedBalance = register.OpeningBalance + register.TotalIncome - register.TotalExpense;
        register.Difference = request.ClosingBalance - register.ExpectedBalance;
        register.ClosedAt = DateTime.UtcNow;
        register.Status = "closed";
        register.Notes = request.Notes ?? register.Notes;

        await _registerRepo.SaveChangesAsync();
        return _mapper.Map<CashRegisterResponse>(register);
    }

    public async Task<CashRegisterResponse?> GetByIdAsync(Guid id)
    {
        var register = await _registerRepo.GetByIdAsync(id);
        return register is null ? null : _mapper.Map<CashRegisterResponse>(register);
    }

    public async Task<CashMovementResponse> AddMovementAsync(CreateCashMovementRequest request)
    {
        var register = await _registerRepo.GetByIdAsync(request.CashRegisterId)
            ?? throw new InvalidOperationException("Cash register not found");

        if (register.Status != "open")
            throw new InvalidOperationException("Cash register is not open");

        var movement = _mapper.Map<CashMovement>(request);
        movement.EmployeeId = _tenant.CurrentEmployeeId;
        movement.CompanyId = Guid.Parse(_tenant.TenantId);
        movement.BranchId = register.BranchId;

        await _movementRepo.AddAsync(movement);

        register.TotalIncome += request.MovementType == "income" ? request.Amount : 0;
        register.TotalExpense += request.MovementType == "expense" ? request.Amount : 0;

        await _movementRepo.SaveChangesAsync();

        return _mapper.Map<CashMovementResponse>(movement);
    }

    public async Task<List<CashMovementResponse>> GetMovementsAsync(Guid cashRegisterId)
    {
        var movements = await _movementRepo.GetByCashRegisterIdAsync(cashRegisterId);
        return _mapper.Map<List<CashMovementResponse>>(movements);
    }

    public async Task<PagedResult<CashRegisterResponse>> GetFilteredAsync(CashRegisterFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;
        var companyId = Guid.Parse(_tenant.TenantId);

        var items = await _registerRepo.GetFilteredAsync(filter.BranchId, filter.Status, filter.FromDate, filter.ToDate, companyId, page, pageSize);
        var total = await _registerRepo.GetFilteredCountAsync(filter.BranchId, filter.Status, filter.FromDate, filter.ToDate, companyId);

        return new PagedResult<CashRegisterResponse>(
            _mapper.Map<List<CashRegisterResponse>>(items),
            total, page, pageSize
        );
    }
}
