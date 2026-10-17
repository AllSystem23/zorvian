using FluentAssertions;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Fase B: los reportes financieros aceptan ?branchId= opcional y resuelven el scope
/// con la semántica estándar (explícito ?? header X-Branch-Id ?? todas). Los saldos de
/// apertura siguen siendo de compañía (documentado en BranchScope).
/// </summary>
public sealed class FinancialReportBranchScopeTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BranchA = Guid.NewGuid();
    private static readonly Guid BranchB = Guid.NewGuid();
    private static readonly Guid PeriodId = Guid.NewGuid();

    private readonly Mock<IAccountingEntryRepository> _entryRepo = new();
    private readonly Mock<IAccountingPeriodRepository> _periodRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly FinancialReportService _sut;

    public FinancialReportBranchScopeTests()
    {
        _tenant.Setup(t => t.SelectedCompanyId).Returns(CompanyId);
        _tenant.Setup(t => t.TenantId).Returns(CompanyId.ToString());

        _sut = new FinancialReportService(
            _entryRepo.Object, _periodRepo.Object, _accountRepo.Object,
            Mock.Of<ICostCenterRepository>(), Mock.Of<IBudgetRepository>(),
            Mock.Of<ICompanyRepository>(), _tenant.Object);

        _periodRepo.Setup(r => r.GetByIdAsync(PeriodId)).ReturnsAsync(new AccountingPeriod
        {
            Id = PeriodId, CompanyId = CompanyId, Name = "Enero 2026", Year = 2026, Month = 1, Status = "open"
        });
        _accountRepo.Setup(r => r.GetAllAsync(CompanyId)).ReturnsAsync(new List<Account>());
        _entryRepo.Setup(r => r.GetFilteredAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<AccountingEntry>());
        _entryRepo.Setup(r => r.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<AccountingEntry>());
    }

    [Fact]
    public async Task TrialBalance_ConHeaderDeSucursal_FiltraEsasEntradas()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(BranchA);

        var result = await _sut.GetTrialBalanceAsync(PeriodId);

        result.Should().NotBeNull();
        _entryRepo.Verify(r => r.GetFilteredAsync(
            PeriodId, null, "posted", null, null, CompanyId, 1, int.MaxValue, BranchA), Times.Once);
    }

    [Fact]
    public async Task TrialBalance_SucursalExplicita_Gana_SobreElHeader()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(BranchA);

        await _sut.GetTrialBalanceAsync(PeriodId, BranchB);

        _entryRepo.Verify(r => r.GetFilteredAsync(
            PeriodId, null, "posted", null, null, CompanyId, 1, int.MaxValue, BranchB), Times.Once);
    }

    [Fact]
    public async Task TrialBalance_SinSucursal_PasaGuidEmpty_TodasLasSucursales()
    {
        var result = await _sut.GetTrialBalanceAsync(PeriodId);

        result.Should().NotBeNull();
        _entryRepo.Verify(r => r.GetFilteredAsync(
            PeriodId, null, "posted", null, null, CompanyId, 1, int.MaxValue, Guid.Empty), Times.Once);
    }

    [Fact]
    public async Task IncomeStatement_HeredaElScopeDeSucursalDelTrialBalance()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(BranchA);

        var result = await _sut.GetIncomeStatementAsync(PeriodId);

        result.Should().NotBeNull();
        _entryRepo.Verify(r => r.GetFilteredAsync(
            PeriodId, null, "posted", null, null, CompanyId, 1, int.MaxValue, BranchA), Times.Once);
    }

    [Fact]
    public async Task GeneralLedger_ConHeaderDeSucursal_FiltraEsasEntradas()
    {
        var accountId = Guid.NewGuid();
        _accountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(new Account
        {
            Id = accountId, Code = "1.1.01", Name = "Caja", NormalSide = "Debit", OpeningBalance = 0
        });
        _tenant.Setup(t => t.CurrentBranchId).Returns(BranchA);

        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        var result = await _sut.GetGeneralLedgerAsync(accountId, from, to);

        result.Should().NotBeNull();
        _entryRepo.Verify(r => r.GetFilteredAsync(
            null, null, "posted", from, to, CompanyId, 1, int.MaxValue, BranchA), Times.Once);
    }

    [Fact]
    public async Task CashFlow_ConHeaderDeSucursal_FiltraEsasEntradas()
    {
        var enhanced = new EnhancedReportService(
            _sut, _entryRepo.Object, _periodRepo.Object, _accountRepo.Object,
            _tenant.Object, Mock.Of<ICompanyRepository>());

        _periodRepo.Setup(r => r.GetByIdAsync(PeriodId)).ReturnsAsync(new AccountingPeriod
        {
            Id = PeriodId, CompanyId = CompanyId, Name = "Enero 2026", Year = 2026, Month = 1, Status = "open"
        });
        _entryRepo.Setup(r => r.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<AccountingEntry>());
        _accountRepo.Setup(r => r.GetAllAsync(CompanyId)).ReturnsAsync(new List<Account>());
        _tenant.Setup(t => t.CurrentBranchId).Returns(BranchA);

        var result = await enhanced.GetCashFlowStatementAsync(PeriodId);

        result.Should().NotBeNull();
        _entryRepo.Verify(r => r.GetFilteredAsync(
            PeriodId, null, "posted", null, null, CompanyId, 1, int.MaxValue, BranchA), Times.Once);
    }
}
