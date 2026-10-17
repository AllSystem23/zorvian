using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.DTOs.ServiceProviderPayment;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Pago a Prestadores de Servicios (fuera de planilla): comisión porcentual sobre
/// recaudo en efectivo por línea, bono fijo SOLO si la meta se cumple (modo "total"
/// por defecto, abierto a modo "individual" por línea), deducciones con referencia
/// de acuerdo OPCIONAL, edición manual en borrador y máquina de estados
/// draft → approved → paid (cancel desde draft/approved).
/// </summary>
public sealed class ServiceProviderPaymentServiceTests
{
    private readonly Mock<IServiceProviderPaymentRepository> _repo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IWebhookService> _webhook = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IAutoAccountingService> _autoAccounting = new();
    private readonly ServiceProviderPaymentService _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _providerId = Guid.NewGuid();

    public ServiceProviderPaymentServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _repo.Setup(r => r.GeneratePaymentNumberAsync(It.IsAny<Guid>()))
            .ReturnsAsync("PP-20261017-0001");
        _repo.Setup(r => r.GetByPeriodAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((ServiceProviderPayment?)null);
        _employeeRepo.Setup(e => e.GetByIdAsync(_providerId))
            .ReturnsAsync(ProviderEmployee());

        _sut = new ServiceProviderPaymentService(
            _repo.Object, _employeeRepo.Object, _tenant.Object, _webhook.Object,
            _autoAccounting.Object,
            new Mock<ILogger<ServiceProviderPaymentService>>().Object);
    }

    // ── Fixtures ──

    private static Employee ProviderEmployee(Guid? branchId = null, Branch? branch = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "María",
        LastName = "Fernández",
        EmployeeCode = "PS-001",
        CollaboratorType = "service_provider",
        BranchId = branchId,
        Branch = branch,
    };

    private static ServiceProviderPaymentLineDto Line(
        string name, decimal collected, decimal contracted, decimal rate) =>
        new(name, collected, contracted, rate);

    private static ServiceProviderPaymentDeductionDto Ded(
        string type, decimal amount, string? agreement = null, string? description = null) =>
        new(type, description, amount, agreement, null);

    private CreateServiceProviderPaymentRequest Req(
        Guid? employeeId = null,
        int year = 2026, int month = 10, int half = 1,
        string? goalMode = "total",
        decimal goalBonus = 1000m,
        decimal otherIncome = 0m,
        List<ServiceProviderPaymentLineDto>? lines = null,
        List<ServiceProviderPaymentDeductionDto>? deductions = null) =>
        new(employeeId ?? _providerId, year, month, half, new DateOnly(2026, 10, 15),
            goalMode, goalBonus, otherIncome, null,
            lines ?? new List<ServiceProviderPaymentLineDto>(),
            deductions ?? new List<ServiceProviderPaymentDeductionDto>());

    private static ServiceProviderPayment Stored(string status, Employee employee, string mode = "total") => new()
    {
        Id = Guid.NewGuid(),
        PaymentNumber = "PP-20261017-0002",
        EmployeeId = employee.Id,
        Employee = employee,
        PeriodYear = 2026,
        PeriodMonth = 10,
        PeriodHalf = 1,
        PaymentDate = new DateOnly(2026, 10, 15),
        GoalEvaluationMode = mode,
        Status = status,
        Lines = new List<ServiceProviderPaymentLine>(),
        Deductions = new List<ServiceProviderPaymentDeduction>(),
    };

    // ── Cálculo ──

    [Fact]
    public async Task Create_ComputesCommissionsGoalAndTotals_MatchingVoucherExample()
    {
        // Matemática exacta del comprobante base (4% sobre recaudo, meta por total):
        // 495,782×4% = 19,831.28 · 54,887×4% = 2,195.48 · acumulado 23,026.76
        // (bono 1,000 + comisiones) · deducciones 13,923.36 · neto 9,103.40.
        var result = await _sut.CreateAsync(Req(
            lines: new List<ServiceProviderPaymentLineDto>
            {
                Line("OTROS", 495782m, 495782m, 4m),
                Line("TELÉFONO", 54887m, 54887m, 4m),
            },
            deductions: new List<ServiceProviderPaymentDeductionDto>
            {
                Ded("advance_first_half", 4000m, "AC-2026-01", "ADELANTO PRIMERA QUINCENA"),
                Ded("store_credit", 5000m, "AC-2026-02", "LAVADORA FRIGIDAIRE SALDO 4692"),
                Ded("cash_loan", 4923.36m, "AC-2026-03", "PRÉSTAMO EN EFECTIVO"),
            }));

        result.Status.Should().Be("draft");
        result.PaymentNumber.Should().Be("PP-20261017-0001");
        result.TotalCollected.Should().Be(550669m);
        result.TotalContracted.Should().Be(550669m);
        result.Lines[0].CommissionAmount.Should().Be(19831.28m, "495,782 × 4%");
        result.Lines[1].CommissionAmount.Should().Be(2195.48m, "54,887 × 4%");
        result.TotalCommission.Should().Be(22026.76m);
        result.GoalMet.Should().BeTrue("recaudo total ≥ contrato total");
        result.TotalAccumulated.Should().Be(23026.76m, "bono 1,000 + comisiones 22,026.76");
        result.TotalDeductions.Should().Be(13923.36m);
        result.NetAmount.Should().Be(9103.40m, "23,026.76 − 13,923.36");
        result.Lines.Should().HaveCount(2);
        result.Deductions.Should().HaveCount(3);
        result.Deductions[1].AgreementReference.Should().Be("AC-2026-02");

        _repo.Verify(r => r.AddAsync(It.Is<ServiceProviderPayment>(p =>
            p.Status == "draft" &&
            p.PaymentNumber == "PP-20261017-0001" &&
            p.GoalMet && p.NetAmount == 9103.40m)), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "service_provider_payment.created");
    }

    [Fact]
    public async Task Create_GoalNotMet_PaysNoBonus()
    {
        var result = await _sut.CreateAsync(Req(
            goalBonus: 1000m,
            lines: new List<ServiceProviderPaymentLineDto> { Line("OTROS", 50000m, 60000m, 4m) }));

        result.GoalMet.Should().BeFalse("el recaudo (50,000) no alcanza el contrato (60,000)");
        result.TotalCommission.Should().Be(2000m, "50,000 × 4%");
        result.TotalAccumulated.Should().Be(2000m, "sin bono: la meta no se cumplió");
        result.NetAmount.Should().Be(2000m);
        result.GoalBonusAmount.Should().Be(1000m, "el bono configurado sí queda registrado");
    }

    [Fact]
    public async Task Create_GoalModeIndividual_AllLinesMustMeet_Individually()
    {
        // OTROS cumple su línea, TELÉFONO no — aunque el TOTAL sí alcanza (200 ≥ 200).
        var lines = new List<ServiceProviderPaymentLineDto>
        {
            Line("OTROS", 150m, 100m, 4m),
            Line("TELÉFONO", 50m, 100m, 4m),
        };

        var byTotal = await _sut.CreateAsync(Req(goalMode: "total", lines: lines));
        byTotal.GoalMet.Should().BeTrue("por TOTAL sí se cumple (200 ≥ 200)");

        var byLine = await _sut.CreateAsync(Req(goalMode: "individual", lines: lines));
        byLine.GoalMet.Should().BeFalse("por LÍNEA individual, TELÉFONO (50 < 100) no cumple");
        byLine.Lines[0].LineGoalMet.Should().BeTrue();
        byLine.Lines[1].LineGoalMet.Should().BeFalse();
        byLine.TotalAccumulated.Should().Be(8m, "solo comisiones: (150+50)×4%, sin bono");
    }

    // ── Validaciones de creación ──

    [Fact]
    public async Task Create_PayrollCollaborator_Throws()
    {
        var payroll = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Juan",
            LastName = "Pérez",
            CollaboratorType = "employee",
        };
        _employeeRepo.Setup(e => e.GetByIdAsync(payroll.Id)).ReturnsAsync(payroll);

        var act = async () => await _sut.CreateAsync(Req(
            employeeId: payroll.Id,
            lines: new List<ServiceProviderPaymentLineDto> { Line("OTROS", 100m, 100m, 4m) }));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*service_provider*");
    }

    [Fact]
    public async Task Create_StoreCreditDeductionWithoutAgreement_IsAccepted()
    {
        // La referencia/fecha del acuerdo es OPCIONAL para todos los tipos: el pago
        // no se bloquea si se deja vacía (se conserva el campo por si se registra).
        var result = await _sut.CreateAsync(Req(
            goalBonus: 0m,
            lines: new List<ServiceProviderPaymentLineDto> { Line("OTROS", 10000m, 5000m, 4m) },
            deductions: new List<ServiceProviderPaymentDeductionDto>
            {
                Ded("store_credit", 350m, agreement: null, description: "LAVADORA FRIGIDAIRE"),
            }));

        result.TotalDeductions.Should().Be(350m, "la deducción sin acuerdo previo se acepta");
        result.NetAmount.Should().Be(50m, "400 − 350");
        result.Deductions.Should().HaveCount(1);
        result.Deductions[0].AgreementReference.Should().BeNull("el acuerdo no se exige");
    }

    [Fact]
    public async Task Create_InssDeduction_DoesNotRequireAgreement()
    {
        var result = await _sut.CreateAsync(Req(
            goalBonus: 0m,
            lines: new List<ServiceProviderPaymentLineDto> { Line("OTROS", 10000m, 5000m, 4m) },
            deductions: new List<ServiceProviderPaymentDeductionDto>
            {
                Ded("inss", 350m, agreement: null, description: "APORTE INSS"),
            }));

        result.TotalCommission.Should().Be(400m, "10,000 × 4%");
        result.TotalDeductions.Should().Be(350m);
        result.NetAmount.Should().Be(50m, "400 − 350: el INSS NO exige acuerdo previo");
        result.GoalMet.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateActivePeriod_Throws()
    {
        _repo.Setup(r => r.GetByPeriodAsync(_providerId, 2026, 10, 1))
            .ReturnsAsync(Stored("draft", ProviderEmployee()));

        var act = async () => await _sut.CreateAsync(Req(
            lines: new List<ServiceProviderPaymentLineDto> { Line("OTROS", 100m, 100m, 4m) }));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Ya existe un pago*");
    }

    [Fact]
    public async Task Create_InvalidCommissionRate_Throws()
    {
        var act = async () => await _sut.CreateAsync(Req(
            lines: new List<ServiceProviderPaymentLineDto> { Line("OTROS", 100m, 100m, 120m) }));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*entre 0 y 100*");
    }

    [Fact]
    public async Task Create_NoLines_Throws()
    {
        var act = async () => await _sut.CreateAsync(Req());

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*al menos una línea*");
    }

    [Fact]
    public async Task Create_InvalidHalf_Throws()
    {
        var act = async () => await _sut.CreateAsync(Req(half: 3,
            lines: new List<ServiceProviderPaymentLineDto> { Line("OTROS", 100m, 100m, 4m) }));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Quincena inválida*");
    }

    // ── Edición manual y máquina de estados ──

    [Fact]
    public async Task Update_OnDraft_RecomputesTotals()
    {
        var employee = ProviderEmployee();
        var stored = Stored("draft", employee);
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        var result = await _sut.UpdateAsync(stored.Id, new UpdateServiceProviderPaymentRequest(
            new DateOnly(2026, 10, 16), "total", 1000m, 250m, "editado manualmente",
            new List<ServiceProviderPaymentLineDto> { Line("OTROS", 80000m, 75000m, 4m) },
            new List<ServiceProviderPaymentDeductionDto>
            {
                Ded("salary_advance", 2000m, "AC-2026-09", "Adelanto"),
            }));

        result.Lines.Should().HaveCount(1, "la edición reconstruye las líneas completas");
        result.TotalCommission.Should().Be(3200m, "80,000 × 4%");
        result.GoalMet.Should().BeTrue("80,000 ≥ 75,000");
        result.TotalAccumulated.Should().Be(4450m, "bono 1,000 + comisión 3,200 + otros 250");
        result.NetAmount.Should().Be(2450m, "4,450 − deducción 2,000");
        result.Notes.Should().Be("editado manualmente");
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Update_OnApproved_Throws()
    {
        var stored = Stored("approved", ProviderEmployee());
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        var act = async () => await _sut.UpdateAsync(stored.Id, new UpdateServiceProviderPaymentRequest(
            new DateOnly(2026, 10, 15), "total", 0m, 0m, null,
            new List<ServiceProviderPaymentLineDto> { Line("OTROS", 1m, 1m, 4m) },
            new List<ServiceProviderPaymentDeductionDto>()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*borrador*approved*");
    }

    [Fact]
    public async Task Approve_FromDraft_TransitionsAndPublishesWebhook()
    {
        var stored = Stored("draft", ProviderEmployee());
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        var result = await _sut.ApproveAsync(stored.Id);

        result.Status.Should().Be("approved");
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "service_provider_payment.approved");
    }

    [Fact]
    public async Task MarkPaid_OnlyFromApproved_ThrowsOnDraft()
    {
        var stored = Stored("draft", ProviderEmployee());
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        var act = async () => await _sut.MarkPaidAsync(stored.Id, new MarkServiceProviderPaymentPaidRequest("ACH-778"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*aprobado*draft*");

        _autoAccounting.Verify(a => a.GenerateServiceProviderPaymentEntryAsync(
            It.IsAny<ServiceProviderPayment>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task MarkPaid_WithAccumulated_GeneratesAccountingEntry()
    {
        var stored = Stored("approved", ProviderEmployee());
        stored.TotalAccumulated = 23026.76m;
        stored.NetAmount = 9103.40m;
        stored.Deductions = new List<ServiceProviderPaymentDeduction>
        {
            new() { PaymentId = stored.Id, DeductionType = "advance_first_half", Amount = 4000m, AgreementReference = "AC-2026-01" },
            new() { PaymentId = stored.Id, DeductionType = "inss", Amount = 9923.36m },
        };
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        await _sut.MarkPaidAsync(stored.Id, new MarkServiceProviderPaymentPaidRequest("ACH-778"));

        _autoAccounting.Verify(a => a.GenerateServiceProviderPaymentEntryAsync(
            It.Is<ServiceProviderPayment>(p => p.Id == stored.Id && p.Deductions.Count == 2),
            It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task MarkPaid_WhenAccountingFails_StillMarksPaid()
    {
        // Best-effort: si faltan enlaces de cuentas (o el período está cerrado), el pago
        // se completa igual y se omite el asiento (la advertencia va al log).
        var stored = Stored("approved", ProviderEmployee());
        stored.TotalAccumulated = 2000m;
        stored.NetAmount = 2000m;
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);
        _autoAccounting.Setup(a => a.GenerateServiceProviderPaymentEntryAsync(
                It.IsAny<ServiceProviderPayment>(), It.IsAny<Guid?>()))
            .ThrowsAsync(new InvalidOperationException("No account linked for ServiceProviderPayment/ServicesExpense"));

        var result = await _sut.MarkPaidAsync(stored.Id, new MarkServiceProviderPaymentPaidRequest("ACH-778"));

        result.Status.Should().Be("paid", "el fallo contable no debe bloquear el pago");
        result.PaidAt.Should().NotBeNull();
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "service_provider_payment.paid");
    }

    [Fact]
    public async Task MarkPaid_WithoutAccumulated_SkipsAccountingEntry()
    {
        var stored = Stored("approved", ProviderEmployee()); // TotalAccumulated = 0 → no hay gasto
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        await _sut.MarkPaidAsync(stored.Id, new MarkServiceProviderPaymentPaidRequest("ACH-778"));

        _autoAccounting.Verify(a => a.GenerateServiceProviderPaymentEntryAsync(
            It.IsAny<ServiceProviderPayment>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task MarkPaid_FromApproved_RecordsPaidAtAndReference()
    {
        var stored = Stored("approved", ProviderEmployee());
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        var result = await _sut.MarkPaidAsync(stored.Id, new MarkServiceProviderPaymentPaidRequest("ACH-778"));

        result.Status.Should().Be("paid");
        result.PaidAt.Should().NotBeNull();
        result.PaymentReference.Should().Be("ACH-778");
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "service_provider_payment.paid");
    }

    [Fact]
    public async Task Cancel_OnPaid_Throws()
    {
        var stored = Stored("paid", ProviderEmployee());
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        var act = async () => await _sut.CancelAsync(stored.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*paid*");
    }

    // ── Historial ──

    [Fact]
    public async Task GetFiltered_MapsItemsWithPeriodLabel()
    {
        var stored = Stored("paid", ProviderEmployee());
        _repo.Setup(r => r.GetFilteredAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<ServiceProviderPayment> { stored }, 1));

        var (items, total) = await _sut.GetFilteredAsync(null, null, 2026, 10, 1, "paid", null, 1, 20);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].PeriodLabel.Should().Be("Quincena 1 · Oct 2026");
        items[0].EmployeeName.Should().Be("María Fernández");
        items[0].EmployeeCode.Should().Be("PS-001");
    }

    // ── Sucursal (reportería) ──

    [Fact]
    public async Task GetFiltered_NoExplicitBranch_UsesContextBranch()
    {
        // Sin filtro explícito se usa la sucursal del contexto (header X-Branch-Id):
        // el listado se acota automáticamente a la sucursal que el usuario tiene activa.
        var contextBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);

        await _sut.GetFilteredAsync(null, null, null, null, null, null, null, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(
            It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<int?>(), It.IsAny<string?>(), contextBranch, It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFiltered_ExplicitBranch_WinsOverContext()
    {
        var explicitBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(Guid.NewGuid());

        await _sut.GetFilteredAsync(null, null, null, null, null, null, explicitBranch, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(
            It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<int?>(), It.IsAny<string?>(), explicitBranch, It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFiltered_NoBranchAnywhere_PassesEmpty_TodasLasSucursales()
    {
        // Sin filtro explícito y sin sucursal en el contexto → Guid.Empty = todas.
        await _sut.GetFilteredAsync(null, null, null, null, null, null, null, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(
            It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<int?>(), It.IsAny<string?>(), Guid.Empty, It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFiltered_MapsBranchFromEmployee()
    {
        // La sucursal del pago se HEREDA del prestador (Employee.BranchId/Branch).
        var branchId = Guid.NewGuid();
        var employee = ProviderEmployee(branchId, new Branch { Id = branchId, Name = "Sucursal Norte" });
        var stored = Stored("paid", employee);
        _repo.Setup(r => r.GetFilteredAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<ServiceProviderPayment> { stored }, 1));

        var (items, _) = await _sut.GetFilteredAsync(null, null, null, null, null, null, null, 1, 20);

        items[0].BranchId.Should().Be(branchId);
        items[0].BranchName.Should().Be("Sucursal Norte");
    }

    // ── Sucursal del asiento contable al pagar ──

    [Fact]
    public async Task MarkPaid_UsesEmployeeBranch_ForAccountingEntry()
    {
        // La transacción pertenece a la sucursal del PRESTADOR (reportería financiera
        // por branch), no a la del usuario que procesa el pago.
        var employeeBranch = Guid.NewGuid();
        var stored = Stored("approved", ProviderEmployee(employeeBranch));
        stored.TotalAccumulated = 5000m;
        stored.NetAmount = 5000m;
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        await _sut.MarkPaidAsync(stored.Id, new MarkServiceProviderPaymentPaidRequest(null));

        _autoAccounting.Verify(a => a.GenerateServiceProviderPaymentEntryAsync(
            It.IsAny<ServiceProviderPayment>(), employeeBranch), Times.Once);
    }

    [Fact]
    public async Task MarkPaid_EmployeeWithoutBranch_FallsBackToContextBranch()
    {
        var contextBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);
        var stored = Stored("approved", ProviderEmployee());
        stored.TotalAccumulated = 5000m;
        stored.NetAmount = 5000m;
        _repo.Setup(r => r.GetByIdAsync(stored.Id)).ReturnsAsync(stored);

        await _sut.MarkPaidAsync(stored.Id, new MarkServiceProviderPaymentPaidRequest(null));

        _autoAccounting.Verify(a => a.GenerateServiceProviderPaymentEntryAsync(
            It.IsAny<ServiceProviderPayment>(), contextBranch), Times.Once);
    }
}
