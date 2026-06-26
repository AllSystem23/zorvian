using AutoMapper;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Credit;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class CreditService
{
    private readonly ICreditRepository _creditRepo;
    private readonly ICreditPaymentRepository _paymentRepo;
    private readonly ILateFeeRepository _lateFeeRepo;
    private readonly ICollectionActionRepository _collectionActionRepo;
    private readonly ICreditRefinancingRepository _refinancingRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ISaleRepository _saleRepo;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public CreditService(
        ICreditRepository creditRepo,
        ICreditPaymentRepository paymentRepo,
        ILateFeeRepository lateFeeRepo,
        ICollectionActionRepository collectionActionRepo,
        ICreditRefinancingRepository refinancingRepo,
        ICompanyRepository companyRepo,
        ISaleRepository saleRepo,
        IAutoAccountingService autoAccounting,
        ITenantContext tenant,
        IMapper mapper)
    {
        _creditRepo = creditRepo;
        _paymentRepo = paymentRepo;
        _lateFeeRepo = lateFeeRepo;
        _collectionActionRepo = collectionActionRepo;
        _refinancingRepo = refinancingRepo;
        _companyRepo = companyRepo;
        _saleRepo = saleRepo;
        _autoAccounting = autoAccounting;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<CreditResponse?> GetByIdAsync(Guid id)
    {
        var credit = await _creditRepo.GetByIdAsync(id);
        return credit is null ? null : _mapper.Map<CreditResponse>(credit);
    }

    public async Task<PagedResult<CreditListResponse>> GetFilteredAsync(CreditFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _creditRepo.GetFilteredAsync(filter.ClientId, filter.Status, filter.Search, Guid.Empty, page, pageSize);
        var total = await _creditRepo.GetFilteredCountAsync(filter.ClientId, filter.Status, filter.Search, Guid.Empty);

        return new PagedResult<CreditListResponse>(
            _mapper.Map<List<CreditListResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<CreditPaymentResponse> RegisterPaymentAsync(CreateCreditPaymentRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");

        var credit = await _creditRepo.GetByIdAsync(request.CreditId)
            ?? throw new KeyNotFoundException("Credit not found");

        decimal principalAmount, interestAmount;

        if (request.CreditInstallmentId.HasValue)
        {
            var installment = credit.Installments
                .FirstOrDefault(i => i.Id == request.CreditInstallmentId.Value)
                ?? throw new KeyNotFoundException("Installment not found");

            var ratio = installment.Amount > 0
                ? installment.PrincipalAmount / installment.Amount
                : 0;

            principalAmount = Math.Min(
                Math.Round(request.Amount * ratio, 2),
                Math.Max(0, installment.PrincipalAmount - installment.PaidAmount));
            interestAmount = request.Amount - principalAmount;

            if (interestAmount < 0) interestAmount = 0;

            installment.PaidAmount += request.Amount;
            installment.Balance = Math.Max(0, installment.Amount - installment.PaidAmount);
            if (installment.Balance <= 0)
                installment.Status = "paid";
        }
        else
        {
            var remainingInterest = credit.InterestAmount - credit.Payments.Sum(p => p.InterestAmount);

            principalAmount = Math.Min(request.Amount, Math.Max(0, credit.Balance - remainingInterest));
            interestAmount = Math.Min(Math.Max(0, remainingInterest), request.Amount - principalAmount);

            if (principalAmount < 0) principalAmount = 0;
            if (interestAmount < 0) interestAmount = 0;
        }

        var payment = new CreditPayment
        {
            CreditId = request.CreditId,
            CreditInstallmentId = request.CreditInstallmentId,
            Amount = request.Amount,
            PrincipalAmount = principalAmount,
            InterestAmount = interestAmount,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber,
            PaymentDate = DateTime.UtcNow,
            BranchId = credit.BranchId,
        };

        credit.PaidAmount += request.Amount;
        credit.Balance = Math.Max(0, credit.TotalAmount - credit.PaidAmount);

        if (credit.Balance <= 0)
            credit.Status = "canceled";

        credit.NextDueDate = credit.Installments
            .Where(i => i.Status == "pending")
            .OrderBy(i => i.DueDate)
            .FirstOrDefault()?.DueDate;

        await _paymentRepo.AddAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        await _autoAccounting.GenerateCreditPaymentEntryAsync(
            payment.Id, credit.Id, principalAmount, interestAmount,
            companyId, credit.BranchId);

        if (credit.SaleId.HasValue)
        {
            var sale = await _saleRepo.GetByIdAsync(credit.SaleId.Value);
            if (sale is not null)
            {
                sale.PaidAmount += principalAmount;
                sale.Balance = Math.Max(0, sale.Total - sale.PaidAmount);
                if (sale.Balance <= 0)
                    sale.Status = "completed";
                await _saleRepo.UpdateAsync(sale);
                await _saleRepo.SaveChangesAsync();
            }
        }

        return _mapper.Map<CreditPaymentResponse>(payment);
    }

    public async Task<PagedResult<CreditPaymentResponse>> GetPaymentsAsync(Guid creditId, int page = 1, int pageSize = 20)
    {
        var items = await _paymentRepo.GetByCreditIdAsync(creditId, page, pageSize);
        var total = await _paymentRepo.GetCountByCreditIdAsync(creditId);

        return new PagedResult<CreditPaymentResponse>(
            _mapper.Map<List<CreditPaymentResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<List<LateFeeResponse>> CalculateLateFeesAsync(Guid creditId, decimal? dailyInterestRate = null)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");

        var credit = await _creditRepo.GetByIdAsync(creditId);
        if (credit is null) return [];

        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);
        var settings = company is not null ? await _companyRepo.GetSettingsAsync(company.Id) : null;

        var rate = dailyInterestRate ?? settings?.LateFeeDailyRate ?? 0.001m;
        var feePct = settings?.LateFeePercentage ?? 0.05m;
        var graceDays = settings?.LateFeeGracePeriod ?? 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var results = new List<LateFeeResponse>();

        foreach (var installment in credit.Installments.Where(i => i.Status == "pending" && i.DueDate < today))
        {
            var daysOverdue = today.DayNumber - installment.DueDate.DayNumber;
            if (daysOverdue <= graceDays) continue;

            var existingLateFee = await _lateFeeRepo.GetByInstallmentAndDateAsync(installment.Id, today);

            if (existingLateFee is not null)
            {
                results.Add(_mapper.Map<LateFeeResponse>(existingLateFee));
                continue;
            }

            var feeAmount = Math.Round(installment.Balance * feePct, 2);
            var interestAmount = Math.Round(installment.Balance * rate * daysOverdue, 2);
            var totalAmount = feeAmount + interestAmount;

            var lateFee = new LateFee
            {
                CreditInstallmentId = installment.Id,
                CreditId = creditId,
                DaysOverdue = daysOverdue,
                FeeAmount = feeAmount,
                InterestAmount = interestAmount,
                TotalAmount = totalAmount,
                PaidAmount = 0,
                Balance = totalAmount,
                Status = "pending",
                CalculatedAt = today,
                BranchId = credit.BranchId,
            };

            await _lateFeeRepo.AddAsync(lateFee);

            if (installment.Status == "pending")
                installment.Status = "late";

            results.Add(_mapper.Map<LateFeeResponse>(lateFee));
        }

        if (results.Any())
        {
            var allOverdue = credit.Installments.All(i => i.Status == "paid" || i.Status == "late");
            if (allOverdue && credit.Status != "defaulted")
                credit.Status = "defaulted";

            await _creditRepo.SaveChangesAsync();
            await _lateFeeRepo.SaveChangesAsync();
        }

        return results;
    }

    public async Task<List<LateFeeResponse>> GetLateFeesAsync(Guid creditId)
    {
        var lateFees = await _lateFeeRepo.GetByCreditIdAsync(creditId);
        return _mapper.Map<List<LateFeeResponse>>(lateFees);
    }

    public async Task<List<OverdueInstallmentResponse>> GetOverdueInstallmentsAsync(Guid creditId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var installments = await _creditRepo.GetInstallmentsByCreditIdAsync(creditId);

        return installments
            .Where(i => i.Status == "pending" && i.DueDate < today)
            .Select(i => new OverdueInstallmentResponse(
                i.Id, i.InstallmentNumber, i.DueDate, i.Amount, i.Balance,
                today.DayNumber - i.DueDate.DayNumber, i.Status))
            .ToList();
    }

    public async Task<CollectionActionResponse> AddCollectionActionAsync(CreateCollectionActionRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");

        var credit = await _creditRepo.GetByIdAsync(request.CreditId)
            ?? throw new KeyNotFoundException("Credit not found");

        var employeeId = _tenant.CurrentEmployeeId
            ?? throw new InvalidOperationException("Employee not identified");

        var action = new CollectionAction
        {
            CreditId = request.CreditId,
            EmployeeId = employeeId,
            ActionType = request.ActionType,
            Description = request.Description,
            ActionDate = DateTime.UtcNow,
            FollowUpDate = request.FollowUpDate,
            ContactPerson = request.ContactPerson,
            ContactPhone = request.ContactPhone,
            PromiseAmount = request.PromiseAmount,
            PromiseDate = request.PromiseDate,
            Result = request.Result,
            Status = "completed",
            BranchId = credit.BranchId,
        };

        await _collectionActionRepo.AddAsync(action);
        await _collectionActionRepo.SaveChangesAsync();

        return _mapper.Map<CollectionActionResponse>(action);
    }

    public async Task<PagedResult<CollectionActionResponse>> GetCollectionActionsAsync(Guid creditId, int page = 1, int pageSize = 20)
    {
        var items = await _collectionActionRepo.GetByCreditIdAsync(creditId, page, pageSize);
        var total = await _collectionActionRepo.GetCountByCreditIdAsync(creditId);

        return new PagedResult<CollectionActionResponse>(
            _mapper.Map<List<CollectionActionResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<CreditRefinancingResponse> CreateRefinancingAsync(Guid creditId, CreateRefinancingRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");

        var credit = await _creditRepo.GetByIdAsync(creditId)
            ?? throw new KeyNotFoundException("Credit not found");

        if (credit.Status == "canceled")
            throw new InvalidOperationException("Cannot refinance a paid credit");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalInterest = Math.Round(request.NewFinancedAmount * request.NewInterestRate / 100 * request.NewInstallmentCount, 2);
        var totalAmount = request.NewFinancedAmount + totalInterest;

        var refinancing = new CreditRefinancing
        {
            CreditId = creditId,
            PreviousBalance = credit.Balance,
            PreviousInterestRate = credit.InterestRate,
            PreviousInstallmentCount = credit.InstallmentCount,
            PreviousInstallmentAmount = credit.InstallmentAmount,
            NewFinancedAmount = request.NewFinancedAmount,
            NewInterestRate = request.NewInterestRate,
            NewInstallmentCount = request.NewInstallmentCount,
            NewInstallmentAmount = request.NewInstallmentAmount,
            NewTotalAmount = totalAmount,
            NewInterestAmount = totalInterest,
            NewStartDate = today,
            NewEndDate = today.AddMonths(request.NewInstallmentCount),
            Reason = request.Reason,
            BranchId = credit.BranchId,
        };

        credit.InterestRate = request.NewInterestRate;
        credit.InstallmentCount = request.NewInstallmentCount;
        credit.InstallmentAmount = request.NewInstallmentAmount;
        credit.TotalAmount = totalAmount;
        credit.InterestAmount = totalInterest;
        credit.FinancedAmount = request.NewFinancedAmount;
        credit.Balance = totalAmount - credit.PaidAmount;
        credit.StartDate = today;
        credit.EndDate = today.AddMonths(request.NewInstallmentCount);
        credit.NextDueDate = today.AddMonths(1);
        credit.Status = "active";

        foreach (var inst in credit.Installments.Where(i => i.Status != "paid"))
            inst.Status = "refinanced";

        for (var i = 1; i <= request.NewInstallmentCount; i++)
        {
            var principalPortion = Math.Round(request.NewFinancedAmount / request.NewInstallmentCount, 2);
            var interestPortion = Math.Round(totalInterest / request.NewInstallmentCount, 2);
            if (i == request.NewInstallmentCount)
            {
                principalPortion = request.NewFinancedAmount - principalPortion * (request.NewInstallmentCount - 1);
                interestPortion = totalInterest - interestPortion * (request.NewInstallmentCount - 1);
            }
            credit.Installments.Add(new CreditInstallment
            {
                CreditId = creditId,
                InstallmentNumber = i,
                DueDate = today.AddMonths(i),
                Amount = principalPortion + interestPortion,
                PrincipalAmount = principalPortion,
                InterestAmount = interestPortion,
                PaidAmount = 0,
                Balance = principalPortion + interestPortion,
                Status = "pending",
                BranchId = credit.BranchId,
            });
        }

        await _refinancingRepo.AddAsync(refinancing);
        await _creditRepo.SaveChangesAsync();

        return _mapper.Map<CreditRefinancingResponse>(refinancing);
    }

    public async Task<List<CreditRefinancingResponse>> GetRefinancingsAsync(Guid creditId)
    {
        var list = await _refinancingRepo.GetByCreditIdAsync(creditId);
        return _mapper.Map<List<CreditRefinancingResponse>>(list);
    }

    public async Task<List<LateFeeResponse>> GetAllLateFeesAsync()
    {
        var lateFees = await _lateFeeRepo.GetAllAsync();
        return _mapper.Map<List<LateFeeResponse>>(lateFees);
    }

    public async Task<List<CreditRefinancingResponse>> GetAllRefinancingsAsync()
    {
        var list = await _refinancingRepo.GetAllAsync();
        return _mapper.Map<List<CreditRefinancingResponse>>(list);
    }

    public async Task<PagedResult<CollectionActionResponse>> GetAllCollectionActionsAsync(int page = 1, int pageSize = 20)
    {
        var items = await _collectionActionRepo.GetAllAsync(page, pageSize);
        var total = await _collectionActionRepo.GetTotalCountAsync();
        return new PagedResult<CollectionActionResponse>(
            _mapper.Map<List<CollectionActionResponse>>(items), total, page, pageSize
        );
    }

    public async Task<OverdueDashboardResponse> GetOverdueDashboardAsync()
    {
        var branchId = Guid.Empty;
        var scalars = await _creditRepo.GetOverdueDashboardScalarsRawAsync(branchId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueInsts = (await _creditRepo.GetCriticalOverdueInstallmentsAsync(branchId, 10))
            .Select(i => new OverdueInstallmentResponse(
                i.Id, i.InstallmentNumber, i.DueDate, i.Amount, i.Balance,
                today.DayNumber - i.DueDate.DayNumber, i.Status))
            .ToList();

        var buckets = new List<OverdueAgingBucket>
        {
            new("1-30 dÃ­as", 1, 30, scalars.Bucket1CreditCount, scalars.Bucket1InstallmentCount, scalars.Bucket1TotalBalance, scalars.Bucket1TotalAmount),
            new("31-60 dÃ­as", 31, 60, scalars.Bucket2CreditCount, scalars.Bucket2InstallmentCount, scalars.Bucket2TotalBalance, scalars.Bucket2TotalAmount),
            new("61-90 dÃ­as", 61, 90, scalars.Bucket3CreditCount, scalars.Bucket3InstallmentCount, scalars.Bucket3TotalBalance, scalars.Bucket3TotalAmount),
            new("90+ dÃ­as", 91, 9999, scalars.Bucket4CreditCount, scalars.Bucket4InstallmentCount, scalars.Bucket4TotalBalance, scalars.Bucket4TotalAmount),
        };

        return new OverdueDashboardResponse(
            scalars.TotalOverdueCredits, scalars.TotalActiveCredits, scalars.TotalPortfolio, scalars.TotalOverdueBalance,
            scalars.TotalPortfolio > 0 ? Math.Round(scalars.MonthlyRecovery / scalars.TotalPortfolio * 100, 2) : 0,
            buckets, pastDueInsts
        );
    }
}
