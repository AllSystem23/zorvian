using AutoMapper;
using Nexora.Application.DTOs.Common;
using Nexora.Application.DTOs.Credit;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Application.Services;

public sealed class CreditService
{
    private readonly ICreditRepository _creditRepo;
    private readonly ICreditPaymentRepository _paymentRepo;
    private readonly ILateFeeRepository _lateFeeRepo;
    private readonly ICollectionActionRepository _collectionActionRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public CreditService(
        ICreditRepository creditRepo,
        ICreditPaymentRepository paymentRepo,
        ILateFeeRepository lateFeeRepo,
        ICollectionActionRepository collectionActionRepo,
        ICompanyRepository companyRepo,
        ITenantContext tenant,
        IMapper mapper)
    {
        _creditRepo = creditRepo;
        _paymentRepo = paymentRepo;
        _lateFeeRepo = lateFeeRepo;
        _collectionActionRepo = collectionActionRepo;
        _companyRepo = companyRepo;
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

        var items = await _creditRepo.GetFilteredAsync(filter.ClientId, filter.Status, Guid.Empty, page, pageSize);
        var total = await _creditRepo.GetFilteredCountAsync(filter.ClientId, filter.Status, Guid.Empty);

        return new PagedResult<CreditListResponse>(
            _mapper.Map<List<CreditListResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<CreditPaymentResponse> RegisterPaymentAsync(CreateCreditPaymentRequest request)
    {
        var credit = await _creditRepo.GetByIdAsync(request.CreditId)
            ?? throw new InvalidOperationException("Credit not found");

        var remainingInterest = credit.InterestAmount - credit.Payments.Sum(p => p.InterestAmount);

        var principalAmount = Math.Min(request.Amount, credit.Balance - remainingInterest);
        var interestAmount = Math.Min(remainingInterest, request.Amount - principalAmount);

        if (principalAmount < 0) principalAmount = 0;
        if (interestAmount < 0) interestAmount = 0;

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
            CompanyId = Guid.Parse(_tenant.TenantId),
            BranchId = credit.BranchId,
        };

        credit.PaidAmount += request.Amount;
        credit.Balance = Math.Max(0, credit.TotalAmount - credit.PaidAmount);

        if (request.CreditInstallmentId.HasValue)
        {
            var installment = credit.Installments
                .FirstOrDefault(i => i.Id == request.CreditInstallmentId.Value);
            if (installment is not null)
            {
                installment.PaidAmount += request.Amount;
                installment.Balance = Math.Max(0, installment.Amount - installment.PaidAmount);
                if (installment.Balance <= 0)
                    installment.Status = "paid";
            }
        }

        if (credit.Balance <= 0)
            credit.Status = "canceled";

        credit.NextDueDate = credit.Installments
            .Where(i => i.Status == "pending")
            .OrderBy(i => i.DueDate)
            .FirstOrDefault()?.DueDate;

        await _paymentRepo.AddAsync(payment);
        await _paymentRepo.SaveChangesAsync();

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
        var credit = await _creditRepo.GetByIdAsync(creditId)
            ?? throw new InvalidOperationException("Credit not found");

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
                CompanyId = Guid.Parse(_tenant.TenantId),
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
        var credit = await _creditRepo.GetByIdAsync(request.CreditId)
            ?? throw new InvalidOperationException("Credit not found");

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
            CompanyId = Guid.Parse(_tenant.TenantId),
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
}
