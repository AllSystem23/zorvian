using AutoMapper;
using Moq;
using FluentAssertions;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Credit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class CreditServiceTests
{
    private readonly Mock<ICreditRepository> _creditRepo = new();
    private readonly Mock<ICreditPaymentRepository> _paymentRepo = new();
    private readonly Mock<ILateFeeRepository> _lateFeeRepo = new();
    private readonly Mock<ICollectionActionRepository> _collectionActionRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreditService _sut;
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();

    public CreditServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _tenant.Setup(t => t.CurrentEmployeeId).Returns(_employeeId);
        _sut = new CreditService(
            _creditRepo.Object,
            _paymentRepo.Object,
            _lateFeeRepo.Object,
            _collectionActionRepo.Object,
            _companyRepo.Object,
            _saleRepo.Object,
            _tenant.Object,
            _mapper.Object);
    }

    private Credit MakeCredit(params CreditInstallment[] installments) => new()
    {
        Id = Guid.NewGuid(),
        CreditNumber = "CR-001",
        ClientId = Guid.NewGuid(),
        FinancedAmount = 10000m,
        InterestRate = 0.15m,
        InstallmentCount = installments.Length > 0 ? installments.Length : 1,
        InstallmentAmount = 1000m,
        TotalAmount = 10000m,
        PaidAmount = 0,
        Balance = 10000m,
        InterestAmount = 1500m,
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(11)),
        Status = "active",
        BranchId = _branchId,
        CompanyId = _companyId,
        Installments = installments.ToList(),
    };

    private CreditInstallment MakeInstallment(int number, DateOnly? dueDate = null, decimal balance = 1000m, string status = "pending") => new()
    {
        Id = Guid.NewGuid(),
        CreditId = Guid.NewGuid(),
        InstallmentNumber = number,
        DueDate = dueDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
        Amount = 1000m,
        PrincipalAmount = 800m,
        InterestAmount = 200m,
        PaidAmount = 0,
        Balance = balance,
        Status = status,
    };

    private LateFee MakeLateFee(Guid creditId, Guid installmentId) => new()
    {
        Id = Guid.NewGuid(),
        CreditInstallmentId = installmentId,
        CreditId = creditId,
        DaysOverdue = 10,
        FeeAmount = 50m,
        InterestAmount = 10m,
        TotalAmount = 60m,
        PaidAmount = 0,
        Balance = 60m,
        Status = "pending",
        CalculatedAt = DateOnly.FromDateTime(DateTime.UtcNow),
        CompanyId = _companyId,
        BranchId = _branchId,
    };

    private CompanySettings MakeSettings() => new()
    {
        Id = Guid.NewGuid(),
        LateFeeDailyRate = 0.001m,
        LateFeePercentage = 0.05m,
        LateFeeGracePeriod = 0,
        CompanyId = Guid.NewGuid(),
    };

    [Fact]
    public async Task CalculateLateFeesAsync_WithOverdueInstallments_CreatesLateFeesAndReturnsResponses()
    {
        var creditId = Guid.NewGuid();
        var installment = MakeInstallment(1, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)));
        var credit = MakeCredit(installment);
        credit.Id = creditId;
        var companyId = Guid.NewGuid();
        var settings = MakeSettings();

        _creditRepo.Setup(r => r.GetByIdAsync(creditId)).ReturnsAsync(credit);
        _companyRepo.Setup(r => r.GetByTenantIdAsync(_companyId.ToString())).ReturnsAsync(new Company { Id = companyId });
        _companyRepo.Setup(r => r.GetSettingsAsync(companyId)).ReturnsAsync(settings);
        _lateFeeRepo.Setup(r => r.GetByInstallmentAndDateAsync(installment.Id, It.IsAny<DateOnly>())).ReturnsAsync((LateFee?)null);

        var expectedResponse = new LateFeeResponse(
            Guid.NewGuid(), installment.Id, creditId, 10, 50m, 10m, 60m, 0, 60m,
            "pending", DateOnly.FromDateTime(DateTime.UtcNow), null, null);
        _mapper.Setup(m => m.Map<LateFeeResponse>(It.IsAny<LateFee>())).Returns(expectedResponse);

        var result = await _sut.CalculateLateFeesAsync(creditId);

        result.Should().NotBeNull();
        result.Should().ContainSingle();
        result[0].Should().Be(expectedResponse);
        _lateFeeRepo.Verify(r => r.AddAsync(It.IsAny<LateFee>()), Times.Once);
        _lateFeeRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _creditRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CalculateLateFeesAsync_WithExistingLateFee_ReturnsExistingMappedResponse()
    {
        var creditId = Guid.NewGuid();
        var installment = MakeInstallment(1, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)));
        var credit = MakeCredit(installment);
        credit.Id = creditId;
        var companyId = Guid.NewGuid();
        var settings = MakeSettings();
        var existingLateFee = MakeLateFee(creditId, installment.Id);

        _creditRepo.Setup(r => r.GetByIdAsync(creditId)).ReturnsAsync(credit);
        _companyRepo.Setup(r => r.GetByTenantIdAsync(_companyId.ToString())).ReturnsAsync(new Company { Id = companyId });
        _companyRepo.Setup(r => r.GetSettingsAsync(companyId)).ReturnsAsync(settings);
        _lateFeeRepo.Setup(r => r.GetByInstallmentAndDateAsync(installment.Id, It.IsAny<DateOnly>())).ReturnsAsync(existingLateFee);

        var expectedResponse = new LateFeeResponse(
            existingLateFee.Id, existingLateFee.CreditInstallmentId, existingLateFee.CreditId,
            existingLateFee.DaysOverdue, existingLateFee.FeeAmount, existingLateFee.InterestAmount,
            existingLateFee.TotalAmount, existingLateFee.PaidAmount, existingLateFee.Balance,
            existingLateFee.Status, existingLateFee.CalculatedAt, existingLateFee.PaidAt, existingLateFee.Notes);
        _mapper.Setup(m => m.Map<LateFeeResponse>(existingLateFee)).Returns(expectedResponse);

        var result = await _sut.CalculateLateFeesAsync(creditId);

        result.Should().ContainSingle();
        result[0].Should().Be(expectedResponse);
        _lateFeeRepo.Verify(r => r.AddAsync(It.IsAny<LateFee>()), Times.Never);
    }

    [Fact]
    public async Task CalculateLateFeesAsync_NoInstallments_ReturnsEmptyList()
    {
        var creditId = Guid.NewGuid();
        var credit = MakeCredit();
        credit.Installments.Clear();

        _creditRepo.Setup(r => r.GetByIdAsync(creditId)).ReturnsAsync(credit);
        _companyRepo.Setup(r => r.GetByTenantIdAsync(_companyId.ToString())).ReturnsAsync((Company?)null);

        var result = await _sut.CalculateLateFeesAsync(creditId);

        result.Should().BeEmpty();
        _lateFeeRepo.Verify(r => r.AddAsync(It.IsAny<LateFee>()), Times.Never);
        _lateFeeRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CalculateLateFeesAsync_InstallmentsExistButNoneOverdue_ReturnsEmptyList()
    {
        var creditId = Guid.NewGuid();
        var futureDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var installment = MakeInstallment(1, futureDueDate);
        var credit = MakeCredit(installment);
        credit.Id = creditId;

        _creditRepo.Setup(r => r.GetByIdAsync(creditId)).ReturnsAsync(credit);
        _companyRepo.Setup(r => r.GetByTenantIdAsync(_companyId.ToString())).ReturnsAsync((Company?)null);

        var result = await _sut.CalculateLateFeesAsync(creditId);

        result.Should().BeEmpty();
        _lateFeeRepo.Verify(r => r.AddAsync(It.IsAny<LateFee>()), Times.Never);
    }

    [Fact]
    public async Task CalculateLateFeesAsync_CreditNotFound_ThrowsInvalidOperationException()
    {
        var creditId = Guid.NewGuid();
        _creditRepo.Setup(r => r.GetByIdAsync(creditId)).ReturnsAsync((Credit?)null);

        var act = () => _sut.CalculateLateFeesAsync(creditId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Credit not found");
    }

    [Fact]
    public async Task GetLateFeesAsync_ReturnsMappedLateFeeList()
    {
        var creditId = Guid.NewGuid();
        var lateFees = new List<LateFee>
        {
            MakeLateFee(creditId, Guid.NewGuid()),
            MakeLateFee(creditId, Guid.NewGuid()),
        };
        var expectedDtos = lateFees.Select(lf => new LateFeeResponse(
            lf.Id, lf.CreditInstallmentId, lf.CreditId, lf.DaysOverdue,
            lf.FeeAmount, lf.InterestAmount, lf.TotalAmount, lf.PaidAmount,
            lf.Balance, lf.Status, lf.CalculatedAt, lf.PaidAt, lf.Notes)).ToList();

        _lateFeeRepo.Setup(r => r.GetByCreditIdAsync(creditId)).ReturnsAsync(lateFees);
        _mapper.Setup(m => m.Map<List<LateFeeResponse>>(lateFees)).Returns(expectedDtos);

        var result = await _sut.GetLateFeesAsync(creditId);

        result.Should().BeEquivalentTo(expectedDtos);
    }

    [Fact]
    public async Task GetLateFeesAsync_NoLateFees_ReturnsEmptyList()
    {
        var creditId = Guid.NewGuid();
        _lateFeeRepo.Setup(r => r.GetByCreditIdAsync(creditId)).ReturnsAsync(new List<LateFee>());
        _mapper.Setup(m => m.Map<List<LateFeeResponse>>(It.IsAny<List<LateFee>>())).Returns(new List<LateFeeResponse>());

        var result = await _sut.GetLateFeesAsync(creditId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOverdueInstallmentsAsync_ReturnsOverdueInstallmentsWithCorrectDaysOverdue()
    {
        var creditId = Guid.NewGuid();
        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var overdueInstallment = MakeInstallment(1, overdueDate, balance: 500m);
        var futureInstallment = MakeInstallment(2, futureDate);
        var paidInstallment = MakeInstallment(3, overdueDate, status: "paid");

        _creditRepo.Setup(r => r.GetInstallmentsByCreditIdAsync(creditId))
            .ReturnsAsync(new List<CreditInstallment> { overdueInstallment, futureInstallment, paidInstallment });

        var result = await _sut.GetOverdueInstallmentsAsync(creditId);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(overdueInstallment.Id);
        result[0].DaysOverdue.Should().Be(10);
        result[0].Balance.Should().Be(500m);
        result[0].Status.Should().Be("pending");
    }

    [Fact]
    public async Task GetOverdueInstallmentsAsync_NoOverdueInstallments_ReturnsEmptyList()
    {
        var creditId = Guid.NewGuid();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var futureInstallment = MakeInstallment(1, futureDate);

        _creditRepo.Setup(r => r.GetInstallmentsByCreditIdAsync(creditId))
            .ReturnsAsync(new List<CreditInstallment> { futureInstallment });

        var result = await _sut.GetOverdueInstallmentsAsync(creditId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddCollectionActionAsync_CreatesActionAndReturnsResponse()
    {
        var creditId = Guid.NewGuid();
        var credit = MakeCredit();
        credit.Id = creditId;
        var request = new CreateCollectionActionRequest(
            creditId, "PHONE_CALL", "Called client", null,
            "Juan Pérez", "555-0100", 500m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), "No answer");

        _creditRepo.Setup(r => r.GetByIdAsync(creditId)).ReturnsAsync(credit);

        var expectedResponse = new CollectionActionResponse(
            Guid.NewGuid(), creditId, _employeeId, "Test Employee",
            "PHONE_CALL", "Called client", DateTime.UtcNow, null,
            "Juan Pérez", "555-0100", 500m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), "completed", "No answer");
        _mapper.Setup(m => m.Map<CollectionActionResponse>(It.IsAny<CollectionAction>())).Returns(expectedResponse);

        var result = await _sut.AddCollectionActionAsync(request);

        result.Should().Be(expectedResponse);
        _collectionActionRepo.Verify(r => r.AddAsync(It.IsAny<CollectionAction>()), Times.Once);
        _collectionActionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddCollectionActionAsync_CreditNotFound_ThrowsInvalidOperationException()
    {
        var request = new CreateCollectionActionRequest(
            Guid.NewGuid(), "PHONE_CALL", null, null, null, null, null, null, null);

        _creditRepo.Setup(r => r.GetByIdAsync(request.CreditId)).ReturnsAsync((Credit?)null);

        var act = () => _sut.AddCollectionActionAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Credit not found");
    }

    [Fact]
    public async Task GetCollectionActionsAsync_ReturnsPagedResults()
    {
        var creditId = Guid.NewGuid();
        var actions = new List<CollectionAction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CreditId = creditId,
                ActionType = "PHONE_CALL",
                Status = "completed",
            },
        };
        var expectedDtos = new List<CollectionActionResponse>
        {
            new(Guid.NewGuid(), creditId, _employeeId, "Test", "PHONE_CALL", null,
                DateTime.UtcNow, null, null, null, null, null, "completed", null),
        };

        _collectionActionRepo.Setup(r => r.GetByCreditIdAsync(creditId, 1, 20)).ReturnsAsync(actions);
        _collectionActionRepo.Setup(r => r.GetCountByCreditIdAsync(creditId)).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<List<CollectionActionResponse>>(actions)).Returns(expectedDtos);

        var result = await _sut.GetCollectionActionsAsync(creditId, 1, 20);

        result.Should().NotBeNull();
        result.Items.Should().BeEquivalentTo(expectedDtos);
        result.Total.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetCollectionActionsAsync_EmptyPage_ReturnsEmptyPage()
    {
        var creditId = Guid.NewGuid();

        _collectionActionRepo.Setup(r => r.GetByCreditIdAsync(creditId, 2, 10)).ReturnsAsync(new List<CollectionAction>());
        _collectionActionRepo.Setup(r => r.GetCountByCreditIdAsync(creditId)).ReturnsAsync(0);
        _mapper.Setup(m => m.Map<List<CollectionActionResponse>>(It.IsAny<List<CollectionAction>>())).Returns(new List<CollectionActionResponse>());

        var result = await _sut.GetCollectionActionsAsync(creditId, 2, 10);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }
}
