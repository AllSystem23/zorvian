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
        _mockVacationRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
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
        _mockVacationRepo.Setup(r => r.GetLeaveBalanceAsync(empId, 2026)).ReturnsAsync(existingBalance);
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId)).ReturnsAsync(employee);
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC")).ReturnsAsync(CreateNicConfig());

        // ACT
        await _sut.PayVacationAsync(request.Id);

        // ASSERT — Taken days incremented by 15
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
}
