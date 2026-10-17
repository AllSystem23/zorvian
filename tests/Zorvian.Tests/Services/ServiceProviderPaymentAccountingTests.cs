using FluentAssertions;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Pago a Prestadores de Servicios — LIBROS CONTABLES: estructura del asiento que
/// GenerateServiceProviderPaymentEntryAsync postea al marcar el pago como pagado.
/// DÉBITO Gasto de Servicios por el acumulado; CRÉDITO Efectivo por el neto; cada
/// deducción abona a INSS por pagar (tipo inss) o a Adelantos por aplicar (resto).
/// Resolución AUTOMÁTICA de cuentas en cascada (enlace → código del plan contable →
/// nombre), sin configuración manual del contador.
/// </summary>
public sealed class ServiceProviderPaymentAccountingTests
{
    private readonly Mock<IAccountingEntryRepository> _entryRepo = new();
    private readonly Mock<IAccountingPeriodRepository> _periodRepo = new();
    private readonly Mock<IAccountLinkRepository> _linkRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly AutoAccountingService _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();

    // Cuentas del plan contable (IDs arbitrarios: los tests solo comparan igualdades).
    private readonly Guid _expenseAcc = Guid.NewGuid();   // 6.1.09 Servicios y Honorarios
    private readonly Guid _cashAcc = Guid.NewGuid();      // 1.1.01 Caja General
    private readonly Guid _advancesAcc = Guid.NewGuid();  // 1.1.06 Adelantos y Préstamos
    private readonly Guid _inssAcc = Guid.NewGuid();      // 2.1.04 INSS Laboral por Pagar

    private AccountingEntry? _saved;

    public ServiceProviderPaymentAccountingTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _periodRepo.Setup(r => r.GetCurrentOpenAsync(_companyId))
            .ReturnsAsync(new AccountingPeriod { Id = _periodId, Status = "open" });
        _entryRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(0);

        // Defaults Moq-safe: sin stub, GetByCodesAsync/GetAllAsync devolverían null y los
        // .FirstOrDefault()/foreach internos tronarían con NRE en vez de seguir la cascada.
        _accountRepo.Setup(r => r.GetByCodesAsync(It.IsAny<string[]>(), _companyId))
            .ReturnsAsync(new List<Account>());
        _accountRepo.Setup(r => r.GetAllAsync(_companyId))
            .ReturnsAsync(new List<Account>());

        _sut = new AutoAccountingService(
            _entryRepo.Object, _periodRepo.Object, _linkRepo.Object,
            new Mock<IAccountingRuleRepository>().Object, _accountRepo.Object,
            _tenant.Object, new Mock<IPayrollRepository>().Object,
            new Mock<ICashMovementRepository>().Object,
            new Mock<IAccountingRuleTemplateRepository>().Object,
            new Mock<ICompanyRepository>().Object,
            new Mock<IFiscalYearRepository>().Object,
            new Mock<ICountryTaxConfigRepository>().Object);
    }

    // ── Fixtures ──

    /// <summary>Números del comprobante de referencia del módulo: acumulado 23,026.76
    /// (bono 1,000 + comisiones 22,026.76), neto 9,103.40.</summary>
    private static ServiceProviderPayment SamplePayment(decimal accumulated, decimal net,
        params (string Type, decimal Amount)[] deductions) => new()
    {
        Id = Guid.NewGuid(),
        PaymentNumber = "PP-20261017-0001",
        EmployeeId = Guid.NewGuid(),
        Employee = new Employee { Id = Guid.NewGuid(), FirstName = "María", LastName = "Fernández" },
        PeriodYear = 2026, PeriodMonth = 10, PeriodHalf = 1,
        PaymentDate = new DateOnly(2026, 10, 15),
        Status = "paid",
        TotalAccumulated = accumulated,
        NetAmount = net,
        Deductions = deductions
            .Select(d => new ServiceProviderPaymentDeduction { PaymentId = Guid.NewGuid(), DeductionType = d.Type, Amount = d.Amount })
            .ToList(),
    };

    private void Link(string transactionType, string role, Guid accountId) =>
        _linkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(transactionType, role, _companyId))
            .ReturnsAsync(new AccountLink { TransactionType = transactionType, Role = role, AccountId = accountId });

    private void CaptureInto()
    {
        _entryRepo.Setup(r => r.AddAsync(It.IsAny<AccountingEntry>()))
            .Callback<AccountingEntry>(e =>
            {
                // En producción EF/BaseEntity asigna el Id al persistir; el mock lo simula.
                if (e.Id == Guid.Empty) e.Id = Guid.NewGuid();
                _saved = e;
            })
            .Returns(Task.CompletedTask);
    }

    // ── Estructura del asiento ──

    [Fact]
    public async Task Generate_FullPaymentWithDeductions_PostsBalancedEntry()
    {
        // Comprobante de referencia: acumulado 23,026.76, neto 9,103.40,
        // deducciones: adelanto 4,000 (Adelantos) + INSS retenido 9,923.36 (Retención).
        Link(TransactionTypes.ServiceProviderPayment, AccountRoles.ServicesExpense, _expenseAcc);
        Link(TransactionTypes.ServiceProviderPayment, AccountRoles.Cash, _cashAcc);
        Link(TransactionTypes.ServiceProviderPayment, AccountRoles.Advances, _advancesAcc);
        Link(TransactionTypes.ServiceProviderPayment, AccountRoles.WithholdingPayable, _inssAcc);
        CaptureInto();

        var payment = SamplePayment(23026.76m, 9103.40m,
            ("advance_first_half", 4000m), ("inss", 9923.36m));

        var entryId = await _sut.GenerateServiceProviderPaymentEntryAsync(payment, _branchId);

        entryId.Should().NotBeEmpty();
        _saved.Should().NotBeNull("el asiento debe persistirse");
        _saved!.Status.Should().Be("posted");
        _saved.ReferenceType.Should().Be(TransactionTypes.ServiceProviderPayment);
        _saved.ReferenceId.Should().Be(payment.Id);
        _saved.AccountingPeriodId.Should().Be(_periodId);
        _saved.BranchId.Should().Be(_branchId, "el asiento hereda la sucursal de la transacción");
        _saved.Description.Should().Contain(payment.PaymentNumber).And.Contain("Fernández");

        // Invariante: debe == haber == acumulado (NetAmount = Acumulado − Deducciones).
        _saved.TotalDebit.Should().Be(23026.76m);
        _saved.TotalCredit.Should().Be(23026.76m);

        _saved.Details.Should().HaveCount(4);
        _saved.Details.Should().ContainSingle(d =>
            d.AccountId == _expenseAcc && d.DebitAmount == 23026.76m && d.CreditAmount == 0m);
        _saved.Details.Should().ContainSingle(d =>
            d.AccountId == _cashAcc && d.CreditAmount == 9103.40m && d.DebitAmount == 0m);
        _saved.Details.Should().ContainSingle(d =>
            d.AccountId == _advancesAcc && d.CreditAmount == 4000m);
        _saved.Details.Should().ContainSingle(d =>
            d.AccountId == _inssAcc && d.CreditAmount == 9923.36m);
        _saved.Details.Should().Contain(d => d.Description == "INSS retenido");
        _saved.Details.Should().OnlyContain(d => d.CompanyId == _companyId);

        _entryRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Generate_WithoutLinks_FallsBackToChartCodes()
    {
        // Sin AccountLinks configurados: el plan contable por defecto resuelve por código
        // (6.1.09 Servicios y Honorarios, 1.1.01 Caja General).
        _accountRepo.Setup(r => r.GetByCodeAsync("6.1.09", _companyId))
            .ReturnsAsync(new Account { Id = _expenseAcc, Code = "6.1.09", Name = "Servicios y Honorarios Profesionales" });
        _accountRepo.Setup(r => r.GetByCodeAsync("1.1.01", _companyId))
            .ReturnsAsync(new Account { Id = _cashAcc, Code = "1.1.01", Name = "Caja General" });
        CaptureInto();

        var payment = SamplePayment(2000m, 2000m); // sin deducciones: neto == acumulado

        await _sut.GenerateServiceProviderPaymentEntryAsync(payment);

        _saved!.Details.Should().HaveCount(2, "solo gasto y efectivo, sin deducciones");
        _saved.Details.Should().ContainSingle(d => d.AccountId == _expenseAcc && d.DebitAmount == 2000m);
        _saved.Details.Should().ContainSingle(d => d.AccountId == _cashAcc && d.CreditAmount == 2000m);
        _saved.TotalDebit.Should().Be(_saved.TotalCredit);
    }

    [Fact]
    public async Task Generate_DeductionWithoutAccounts_FallsBackToExpenseAccount()
    {
        // Solo resuelven gasto y efectivo: la deducción (que no es INSS) cae a la MISMA
        // cuenta de gasto — reduce el gasto neto y el asiento sigue balanceado sin
        // configuración manual del contador.
        Link(TransactionTypes.ServiceProviderPayment, AccountRoles.ServicesExpense, _expenseAcc);
        Link(TransactionTypes.ServiceProviderPayment, AccountRoles.Cash, _cashAcc);
        CaptureInto();

        var payment = SamplePayment(1000m, 500m, ("store_credit", 500m));

        await _sut.GenerateServiceProviderPaymentEntryAsync(payment);

        _saved!.Details.Should().HaveCount(3);
        _saved.Details.Should().ContainSingle(d =>
            d.AccountId == _expenseAcc && d.DebitAmount == 1000m);
        _saved.Details.Should().ContainSingle(d =>
            d.AccountId == _cashAcc && d.CreditAmount == 500m);
        _saved.Details.Should().ContainSingle(d =>
            d.AccountId == _expenseAcc && d.CreditAmount == 500m && d.DebitAmount == 0m);
        _saved.TotalDebit.Should().Be(_saved.TotalCredit);
    }

    [Fact]
    public async Task Generate_WithholdingDeduction_UsesPayrollInssLinkWhenOwnLinkMissing()
    {
        // La deducción INSS sin enlace propio reutiliza el enlace de nómina
        // (Payroll/EmployeeInssPayable → INSS Laboral por pagar).
        Link(TransactionTypes.ServiceProviderPayment, AccountRoles.ServicesExpense, _expenseAcc);
        Link(TransactionTypes.ServiceProviderPayment, AccountRoles.Cash, _cashAcc);
        Link(TransactionTypes.Payroll, "EmployeeInssPayable", _inssAcc);
        CaptureInto();

        var payment = SamplePayment(9923.36m, 0m, ("inss", 9923.36m));

        await _sut.GenerateServiceProviderPaymentEntryAsync(payment);

        _saved!.Details.Should().HaveCount(2,
            "neto cero: solo gasto (débito) e INSS retenido (crédito), sin línea de efectivo");
        _saved.Details.Should().ContainSingle(d =>
            d.AccountId == _inssAcc && d.CreditAmount == 9923.36m);
        _saved.Details.Should().NotContain(d => d.AccountId == _cashAcc,
            "el neto es cero: no se emite línea de efectivo");
        _saved.TotalDebit.Should().Be(_saved.TotalCredit);
    }

    // ── Guardas ──

    [Fact]
    public async Task Generate_WithoutResolvableExpenseAccount_ThrowsWithActionableMessage()
    {
        // Sin enlaces ni plan contable: la falla es explícita y accionable (el caller
        // de MarkPaidAsync la captura como InvalidOperationException y solo loguea).
        var act = async () => await _sut.GenerateServiceProviderPaymentEntryAsync(
            SamplePayment(1000m, 1000m));

        var ex = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;
        ex.Message.Should().Contain("6.1.09").And.Contain(AccountRoles.ServicesExpense);
    }

    [Fact]
    public async Task Generate_WithoutAccumulated_Throws()
    {
        Link(TransactionTypes.ServiceProviderPayment, AccountRoles.ServicesExpense, _expenseAcc);

        var act = async () => await _sut.GenerateServiceProviderPaymentEntryAsync(
            SamplePayment(0m, 0m));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*acumulado*");
    }
}