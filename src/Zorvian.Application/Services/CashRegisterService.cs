using AutoMapper;
using Zorvian.Application.DTOs.CashRegister;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class CashRegisterService
{
    private readonly ICashRegisterRepository _registerRepo;
    private readonly ICashMovementRepository _movementRepo;
    private readonly ICashRegisterArqueoRepository _arqueoRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly IAutoAccountingService _accountingService;

    public CashRegisterService(
        ICashRegisterRepository registerRepo,
        ICashMovementRepository movementRepo,
        ICashRegisterArqueoRepository arqueoRepo,
        ITenantContext tenant,
        IMapper mapper,
        IAutoAccountingService accountingService)
    {
        _registerRepo = registerRepo;
        _movementRepo = movementRepo;
        _arqueoRepo = arqueoRepo;
        _tenant = tenant;
        _mapper = mapper;
        _accountingService = accountingService;
    }

    public async Task<bool> ApproveMovementAsync(Guid movementId)
    {
        var movement = await _movementRepo.GetByIdAsync(movementId)
            ?? throw new KeyNotFoundException("Movement not found");

        if (movement.ApprovalStatus == "approved")
            throw new InvalidOperationException("Movement already approved");

        movement.ApprovalStatus = "approved";
        await _movementRepo.UpdateAsync(movement);
        await _movementRepo.SaveChangesAsync();

        await _accountingService.GenerateCashMovementEntryAsync(movementId);

        return true;
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
            ?? throw new KeyNotFoundException("Cash register not found");

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
            ?? throw new KeyNotFoundException("Cash register not found");

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

        // Audit recommendation: Generate accounting entry for cash movements
        if (movement.ApprovalStatus == "approved")
        {
            await _accountingService.GenerateCashMovementEntryAsync(movement.Id);
        }

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

    public async Task<CashRegisterArqueoResponse> CreateArqueoAsync(Guid cashRegisterId, CreateArqueoRequest request)
    {
        var register = await _registerRepo.GetByIdAsync(cashRegisterId)
            ?? throw new KeyNotFoundException("Cash register not found");

        if (register.Status != "open")
            throw new InvalidOperationException("Cash register is already closed");

        var employeeId = _tenant.CurrentEmployeeId
            ?? throw new InvalidOperationException("Employee not identified");

        var expectedBalance = register.OpeningBalance + register.TotalIncome - register.TotalExpense;
        var countedTotal = request.Denominations.Sum(d => d.DenominationValue * d.Quantity);
        var difference = countedTotal - expectedBalance;

        var companyId = Guid.Parse(_tenant.TenantId);

        var arqueo = new CashRegisterArqueo
        {
            CashRegisterId = cashRegisterId,
            ExpectedBalance = expectedBalance,
            CountedTotal = countedTotal,
            Difference = difference,
            Notes = request.Notes,
            EmployeeId = employeeId,
            CompanyId = companyId,
            BranchId = register.BranchId,
        };

        foreach (var d in request.Denominations)
        {
            arqueo.Denominations.Add(new CashArqueoDenomination
            {
                ArqueoId = arqueo.Id,
                DenominationType = d.DenominationType,
                DenominationValue = d.DenominationValue,
                Quantity = d.Quantity,
                CompanyId = companyId,
                BranchId = register.BranchId,
            });
        }

        register.ClosingBalance = countedTotal;
        register.ExpectedBalance = expectedBalance;
        register.Difference = difference;
        register.ClosedAt = DateTime.UtcNow;
        register.Status = "closed";
        register.Notes = request.Notes ?? register.Notes;

        await _arqueoRepo.AddAsync(arqueo);
        await _registerRepo.SaveChangesAsync();

        return _mapper.Map<CashRegisterArqueoResponse>(arqueo);
    }

    public async Task<CashRegisterArqueoResponse?> GetArqueoAsync(Guid cashRegisterId)
    {
        var arqueo = await _arqueoRepo.GetByRegisterIdAsync(cashRegisterId);
        return arqueo is null ? null : _mapper.Map<CashRegisterArqueoResponse>(arqueo);
    }
}
