using AutoMapper;
using FluentAssertions;
using MassTransit;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Regresión del bug de Fase A: GetOverdueDashboardAsync hardcodeaba Guid.Empty.
/// El SQL del repo usaba igualdad exacta (sin guard), así que el dashboard de mora
/// devolvía ~ceros cuando los créditos sí traían BranchId. Ahora debe resolver la
/// sucursal del header (X-Branch-Id) y pasarla al repo; sin sucursal → Guid.Empty
/// (= todas, guard del repo).
/// </summary>
public sealed class OverdueDashboardBranchTests
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

    public OverdueDashboardBranchTests()
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
            new Mock<ITenantContextWriter>().Object,
            _mapper.Object,
            _publishEndpoint.Object);
    }

    [Fact]
    public async Task GetOverdueDashboardAsync_WithBranchInContext_PassesResolvedBranchToRepo()
    {
        var branchId = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(branchId);
        Guid? captured = null;
        _creditRepo.Setup(r => r.GetOverdueDashboardScalarsRawAsync(It.IsAny<Guid>()))
            .Callback<Guid>(b => captured = b)
            .ReturnsAsync(new Zorvian.Application.DTOs.Credit.OverdueDashboardScalars());
        _creditRepo.Setup(r => r.GetCriticalOverdueInstallmentsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(new List<CreditInstallment>());

        await _sut.GetOverdueDashboardAsync();

        captured.Should().Be(branchId, "la sucursal del header debe acotar el dashboard de mora");
    }

    [Fact]
    public async Task GetOverdueDashboardAsync_WithoutBranch_PassesEmptyGuid_AllBranches()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);
        Guid? capturedBranch = null;
        Guid? capturedInst = null;
        _creditRepo.Setup(r => r.GetOverdueDashboardScalarsRawAsync(It.IsAny<Guid>()))
            .Callback<Guid>(b => capturedBranch = b)
            .ReturnsAsync(new Zorvian.Application.DTOs.Credit.OverdueDashboardScalars());
        _creditRepo.Setup(r => r.GetCriticalOverdueInstallmentsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .Callback<Guid, int>((b, _) => capturedInst = b)
            .ReturnsAsync(new List<CreditInstallment>());

        await _sut.GetOverdueDashboardAsync();

        capturedBranch.Should().Be(Guid.Empty, "sin sucursal seleccionada el dashboard es company-wide");
        capturedInst.Should().Be(Guid.Empty);
    }
}
