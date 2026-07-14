using AutoMapper;
using MassTransit;
using Moq;
using Xunit;
using Zorvian.Application.DTOs.Credit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public sealed class CreditServiceTests
{
    private readonly Mock<ICreditRepository> _creditRepo = new();
    private readonly Mock<ICreditPaymentRepository> _paymentRepo = new();
    private readonly Mock<ILateFeeRepository> _lateFeeRepo = new();
    private readonly Mock<ICollectionActionRepository> _collectionActionRepo = new();
    private readonly Mock<ICreditRefinancingRepository> _refinancingRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<IAutoAccountingService> _autoAccounting = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly CreditService _sut;

    public CreditServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _sut = new CreditService(
            _creditRepo.Object,
            _paymentRepo.Object,
            _lateFeeRepo.Object,
            _collectionActionRepo.Object,
            _refinancingRepo.Object,
            _companyRepo.Object,
            _saleRepo.Object,
            _autoAccounting.Object,
            _tenant.Object,
            _mapper.Object,
            _publishEndpoint.Object);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _creditRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Credit?)null);

        var result = await _sut.GetByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsMappedResponse()
    {
        var id = Guid.NewGuid();
        var credit = new Credit { Id = id, Status = "active", CreditNumber = "C-001", FinancedAmount = 1000,
            InterestRate = 12, InstallmentCount = 6, InstallmentAmount = 180,
            TotalAmount = 1080, PaidAmount = 0, Balance = 1080, InterestAmount = 80,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            CurrencyCode = "NIO"
        };
        _creditRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(credit);
        var expected = new CreditResponse(
            id, "C-001", Guid.Empty, "Test Client", null, null, null,
            1000, 12, 6, 180, 1080, 0, 1080, 80,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            null, "active", null, "NIO", null, []);
        _mapper.Setup(m => m.Map<CreditResponse>(credit)).Returns(expected);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("active", result!.Status);
    }
}
