using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public class VacationPaymentServiceTests
{
    private readonly Mock<IVacationRepository> _mockVacationRepo = new();
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo = new();
    private readonly Mock<ICountryTaxConfigRepository> _mockTaxConfigRepo = new();
    private readonly Mock<IPayrollRepository> _mockPayrollRepo = new();
    private readonly Mock<ITenantContext> _mockTenant = new();
    private readonly Mock<ILogger<VacationPaymentService>> _mockLogger = new();
    private readonly VacationPaymentService _sut;

    public VacationPaymentServiceTests()
    {
        _mockTenant.Setup(t => t.TenantId).Returns(Zorvian.Core.Entities.TenantId.FromGuid(Guid.NewGuid()));
        _sut = new VacationPaymentService(
            _mockVacationRepo.Object,
            _mockEmployeeRepo.Object,
            _mockTaxConfigRepo.Object,
            _mockPayrollRepo.Object,
            _mockTenant.Object,
            _mockLogger.Object);
    }

    private VacationRequest CreateApprovedRequest(Guid empId) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = empId,
        StartDate = new DateOnly(2026, 1, 5),
        EndDate = new DateOnly(2026, 1, 19),
        TotalDays = 15,
        BusinessDays = 15,
        Status = "approved",
    };

    private Employee CreateEmployee(Guid id, decimal salary = 11000m) => new()
    {
        Id = id,
        FirstName = "Wilhen",
        LastName = "Argenis Arguello Blandón",
        IdentificationNumber = "601-110393-0000Q",
        Salary = salary,
        HireDate = new DateOnly(2021, 4, 1),
        CountryCode = "NIC",
        SalaryType = "monthly",
    };

    private CountryTaxConfig CreateNicConfig() => new()
    {
        InssEmployeeRate = 0.07m,
        Currency = "NIO",
        VacationDaysPerYear = 30,
    };

    [Fact]
    public async Task PayVacation_ApprovedRequest_ShouldCreateReceipt()
    {
        // ARRANGE
        var empId = Guid.NewGuid();
        var request = CreateApprovedRequest(empId);
        var employee = CreateEmployee(empId);

        _mockVacationRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _mockVacationRepo.Setup(r => r.GetReceiptByRequestIdAsync(request.Id)).ReturnsAsync((VacationPaymentReceipt?)null);
        _mockVacationRepo.Setup(r => r.GetReceiptCountAsync()).ReturnsAsync(0);
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId)).ReturnsAsync(employee);
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC")).ReturnsAsync(CreateNicConfig());

        // ACT
        var result = await _sut.PayVacationAsync(request.Id);

        // ASSERT
        Assert.NotNull(result);
        Assert.StartsWith("RV-", result.ReceiptNumber);
        Assert.Equal(15, result.BusinessDays);
        // Daily salary = round(11000/30, 2) = 366.67
        Assert.Equal(366.67m, result.DailySalary);
        // Vacation pay = 15 * 366.67 = 5500.05
        Assert.Equal(5500.05m, result.VacationPay);
        // INSS = 7% of vacation pay
        Assert.Equal(0.07m, result.InssEmployeeRate);
        Assert.True(result.InssEmployeeAmount > 0);
        // Net = vacationPay - inss
        Assert.Equal(result.VacationPay - result.InssEmployeeAmount, result.NetPay);
        // Status
        Assert.Equal("paid", result.Status);
        // Request marked as taken
        Assert.Equal("taken", request.Status);
        // Receipt saved
        _mockVacationRepo.Verify(r => r.AddReceiptAsync(It.IsAny<VacationPaymentReceipt>()), Times.Once);
        // 2 llamadas: recálculo de LeaveBalances (LeaveBalanceSync) + guardado del recibo.
        _mockVacationRepo.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task PayVacation_NotApproved_ShouldThrow()
    {
        // ARRANGE
        var empId = Guid.NewGuid();
        var request = CreateApprovedRequest(empId);
        request.Status = "pending";

        _mockVacationRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);

        // ACT & ASSERT
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PayVacationAsync(request.Id));
        Assert.Contains("aprobada", ex.Message);
    }

    [Fact]
    public async Task PayVacation_AlreadyPaid_ShouldThrow()
    {
        // ARRANGE
        var empId = Guid.NewGuid();
        var request = CreateApprovedRequest(empId);
        var existingReceipt = new VacationPaymentReceipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = "RV-20260101-0001",
            VacationRequestId = request.Id,
        };

        _mockVacationRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _mockVacationRepo.Setup(r => r.GetReceiptByRequestIdAsync(request.Id)).ReturnsAsync(existingReceipt);

        // ACT & ASSERT
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PayVacationAsync(request.Id));
        Assert.Contains("ya fue pagada", ex.Message);
    }

    [Fact]
    public async Task PayVacation_RequestNotFound_ShouldThrow()
    {
        // ARRANGE
        var fakeId = Guid.NewGuid();
        _mockVacationRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync((VacationRequest?)null);

        // ACT & ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.PayVacationAsync(fakeId));
    }

    [Fact]
    public async Task PayVacation_ShouldUpdateLeaveBalances()
    {
        // ARRANGE
        var empId = Guid.NewGuid();
        var request = CreateApprovedRequest(empId);
        var employee = CreateEmployee(empId);
        var existingBalance = new LeaveBalances
        {
            Id = Guid.NewGuid(),
            EmployeeId = empId,
            Year = 2026,
            VacationDaysAccrued = 2.5m,
            VacationDaysTaken = 0,
        };

        _mockVacationRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _mockVacationRepo.Setup(r => r.GetReceiptByRequestIdAsync(request.Id)).ReturnsAsync((VacationPaymentReceipt?)null);
        _mockVacationRepo.Setup(r => r.GetReceiptCountAsync()).ReturnsAsync(0);
        _mockVacationRepo.Setup(r => r.GetVacationBalanceSumsAsync(empId, 2026))
            .ReturnsAsync(new LeaveBalanceSums(15m, 0m));
        _mockVacationRepo.Setup(r => r.GetLeaveBalanceAsync(empId, 2026)).ReturnsAsync(existingBalance);
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId)).ReturnsAsync(employee);
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC")).ReturnsAsync(CreateNicConfig());

        // ACT
        await _sut.PayVacationAsync(request.Id);

        // ASSERT — el espejo se RECALCULA desde las solicitudes (no se incrementa a mano):
        // taken = 15 (la solicitud pasó a "taken").
        Assert.Equal(15m, existingBalance.VacationDaysTaken);
    }

    [Fact]
    public async Task PayVacation_CreatesBalanceIfMissing()
    {
        // ARRANGE
        var empId = Guid.NewGuid();
        var request = CreateApprovedRequest(empId);
        var employee = CreateEmployee(empId);

        _mockVacationRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _mockVacationRepo.Setup(r => r.GetReceiptByRequestIdAsync(request.Id)).ReturnsAsync((VacationPaymentReceipt?)null);
        _mockVacationRepo.Setup(r => r.GetReceiptCountAsync()).ReturnsAsync(0);
        _mockVacationRepo.Setup(r => r.GetVacationBalanceSumsAsync(empId, 2026))
            .ReturnsAsync(new LeaveBalanceSums(15m, 0m));
        _mockVacationRepo.Setup(r => r.GetLeaveBalanceAsync(empId, 2026)).ReturnsAsync((LeaveBalances?)null);
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId)).ReturnsAsync(employee);
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC")).ReturnsAsync(CreateNicConfig());

        // ACT
        await _sut.PayVacationAsync(request.Id);

        // ASSERT — New balance created
        _mockVacationRepo.Verify(r => r.AddLeaveBalanceAsync(It.Is<LeaveBalances>(
            b => b.EmployeeId == empId && b.Year == 2026 && b.VacationDaysTaken == 15m)), Times.Once);
    }

    [Fact]
    public async Task PayVacation_ReceiptNumber_ShouldBeSequential()
    {
        // ARRANGE
        var empId = Guid.NewGuid();
        var request = CreateApprovedRequest(empId);
        var employee = CreateEmployee(empId);

        _mockVacationRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _mockVacationRepo.Setup(r => r.GetReceiptByRequestIdAsync(request.Id)).ReturnsAsync((VacationPaymentReceipt?)null);
        _mockVacationRepo.Setup(r => r.GetReceiptCountAsync()).ReturnsAsync(5); // 5 existing receipts
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId)).ReturnsAsync(employee);
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC")).ReturnsAsync(CreateNicConfig());

        // ACT
        var result = await _sut.PayVacationAsync(request.Id);

        // ASSERT — Should be RV-YYYYMMDD-0006
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        Assert.Equal($"RV-{today}-0006", result.ReceiptNumber);
    }

    [Fact]
    public async Task PayVacation_HondurasINSS_ShouldUseCountryRate()
    {
        // ARRANGE — HND uses different INSS rate
        var empId = Guid.NewGuid();
        var request = CreateApprovedRequest(empId);
        var employee = CreateEmployee(empId);
        employee.CountryCode = "HND";
        employee.Salary = 25000m;

        var hndConfig = new CountryTaxConfig
        {
            InssEmployeeRate = 0.05m, // HND: 5%
            Currency = "HNL",
            VacationDaysPerYear = 10,
        };

        _mockVacationRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _mockVacationRepo.Setup(r => r.GetReceiptByRequestIdAsync(request.Id)).ReturnsAsync((VacationPaymentReceipt?)null);
        _mockVacationRepo.Setup(r => r.GetReceiptCountAsync()).ReturnsAsync(0);
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId)).ReturnsAsync(employee);
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("HND")).ReturnsAsync(hndConfig);

        // ACT
        var result = await _sut.PayVacationAsync(request.Id);

        // ASSERT
        Assert.Equal("HND", result.CountryCode);
        Assert.Equal("HNL", result.Currency);
        Assert.Equal(0.05m, result.InssEmployeeRate);
    }

    // ── Anti doble pago con nómina ──

    [Fact]
    public async Task PayVacation_ActivePayrollRunOverlapping_ThrowsToAvoidDoublePay()
    {
        var empId = Guid.NewGuid();
        var request = CreateApprovedRequest(empId);

        _mockVacationRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _mockVacationRepo.Setup(r => r.GetReceiptByRequestIdAsync(request.Id))
            .ReturnsAsync((VacationPaymentReceipt?)null);
        _mockPayrollRepo.Setup(r => r.GetPeriodsAsync(null)).ReturnsAsync(new List<PayrollPeriod>
        {
            new() { Id = Guid.NewGuid(), Name = "Enero 2026", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) },
        });
        _mockPayrollRepo.Setup(r => r.GetRunsAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PayrollRun> { new() { Id = Guid.NewGuid(), Status = "approved" } });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PayVacationAsync(request.Id));

        // El período solapa las vacaciones (5–19 ene) y tiene corrida activa → bloqueado.
        Assert.Contains("corrida de nómina activa", ex.Message);
        _mockVacationRepo.Verify(r => r.AddReceiptAsync(It.IsAny<VacationPaymentReceipt>()), Times.Never);
    }

    [Fact]
    public async Task PayVacation_CancelledRunOverlapping_DoesNotBlock()
    {
        var empId = Guid.NewGuid();
        var request = CreateApprovedRequest(empId);
        var employee = CreateEmployee(empId);

        _mockVacationRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _mockVacationRepo.Setup(r => r.GetReceiptByRequestIdAsync(request.Id))
            .ReturnsAsync((VacationPaymentReceipt?)null);
        _mockPayrollRepo.Setup(r => r.GetPeriodsAsync(null)).ReturnsAsync(new List<PayrollPeriod>
        {
            new() { Id = Guid.NewGuid(), Name = "Enero 2026", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) },
        });
        // Corrida cancelada → revirtió las vacaciones, el pago por recibo vuelve a ser válido.
        _mockPayrollRepo.Setup(r => r.GetRunsAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(new List<PayrollRun> { new() { Id = Guid.NewGuid(), Status = "cancelled" } });
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId)).ReturnsAsync(employee);
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC")).ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetReceiptCountAsync()).ReturnsAsync(0);
        _mockVacationRepo.Setup(r => r.GetVacationBalanceSumsAsync(empId, request.StartDate.Year))
            .ReturnsAsync(new LeaveBalanceSums(0m, 15m));
        _mockVacationRepo.Setup(r => r.GetLeaveBalanceAsync(empId, request.StartDate.Year))
            .ReturnsAsync(new LeaveBalances { Id = Guid.NewGuid(), EmployeeId = empId, Year = request.StartDate.Year });

        var receipt = await _sut.PayVacationAsync(request.Id);

        Assert.NotNull(receipt);
        _mockVacationRepo.Verify(r => r.AddReceiptAsync(It.IsAny<VacationPaymentReceipt>()), Times.Once);
    }
}
