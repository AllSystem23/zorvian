using Moq;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Zorvian.Infrastructure.Data;
using Zorvian.Application.Services.PayrollStrategies;

namespace Zorvian.Tests.PayrollIntegrationTests;

public class Phase7IntegrationTests
{
    private readonly Mock<IPayrollRepository> _mockRepo = new();
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo = new();
    private readonly Mock<ITenantContext> _mockTenant = new();
    private readonly Mock<IWebhookService> _mockWebhook = new();
    private readonly Mock<IAchExportService> _mockAch = new();
    private readonly Mock<IAutoAccountingService> _mockAutoAccounting = new();
    private readonly Mock<ICountryTaxConfigRepository> _mockTaxConfigRepo = new();
    private readonly Mock<ICompanyRepository> _mockCompanyRepo = new();
    private readonly Mock<IOvertimeRecordRepository> _mockOvertimeRepo = new();
    private readonly Mock<ICommissionRecordRepository> _mockCommissionRepo = new();
    private readonly Mock<IBonusRecordRepository> _mockBonusRepo = new();
    private readonly Mock<IPayrollConceptRepository> _mockConceptRepo = new();
    private readonly Mock<IEmployeeLoanRepository> _mockLoanRepo = new();
    private readonly Mock<ISalaryAdvanceRepository> _mockAdvanceRepo = new();
    private readonly Mock<IWageGarnishmentRepository> _mockGarnishmentRepo = new();
    private readonly Mock<IBenefitProvisionRepository> _mockBenefitProvisionRepo = new();
    private readonly Mock<IAuditLogRepository> _mockAuditRepo = new();
    private readonly Mock<ISickLeaveRepository> _mockSickLeaveRepo = new();
    private readonly PayrollCalculationFactory _factory;

    public Phase7IntegrationTests()
    {
        _factory = new PayrollCalculationFactory(new List<IPayrollCalculationStrategy> { new NicaraguaCalculationStrategy() });
    }

    private PayrollService CreateService()
    {
        return new PayrollService(
            _mockRepo.Object, _mockEmployeeRepo.Object, _mockTenant.Object, _mockWebhook.Object,
            _mockAch.Object, _mockAutoAccounting.Object, _mockTaxConfigRepo.Object, _mockCompanyRepo.Object,
            _mockOvertimeRepo.Object, _mockCommissionRepo.Object, _mockBonusRepo.Object,
            _mockConceptRepo.Object, _mockLoanRepo.Object, _mockAdvanceRepo.Object, _mockGarnishmentRepo.Object,
            _mockBenefitProvisionRepo.Object, _mockAuditRepo.Object, _factory, 
            new Mock<IBankTransferService>().Object, _mockSickLeaveRepo.Object);
    }

    [Fact]
    public async Task UpdateDetailAsync_ShouldLogAuditTrail()
    {
        // ARRANGE
        var service = CreateService();
        var detailId = Guid.NewGuid();
        var detail = new PayrollDetail { Id = detailId, BaseSalary = 1000m, GrossPay = 1000m };
        
        _mockRepo.Setup(r => r.GetDetailByIdAsync(detailId)).ReturnsAsync(detail);
        _mockTenant.Setup(t => t.CurrentEmployeeId).Returns(Guid.NewGuid());

        var request = new UpdatePayrollDetailRequest(1200m, null, null, null, null, null, null, null);

        // ACT
        await service.UpdateDetailAsync(detailId, request);

        // ASSERT
        _mockAuditRepo.Verify(a => a.AddAsync(It.Is<AuditLog>(l => 
            l.EntityName == "PayrollDetail" && 
            l.EntityId == detailId.ToString() &&
            l.Action == "UPDATE")), Times.Once);
    }

    [Fact]
    public async Task ApproveRunAsync_ShouldRequireMultipleStepsForHighAmount()
    {
        // ARRANGE
        var service = CreateService();
        var runId = Guid.NewGuid();
        var run = new PayrollRun { Id = runId, Status = "draft", TotalNetPay = 60000m };
        
        _mockRepo.Setup(r => r.GetRunByIdAsync(runId)).ReturnsAsync(run);

        // ACT - Submit for approval
        await service.ApproveRunAsync(runId);

        // ASSERT
        Assert.Equal("pending_approval", run.Status);
        Assert.Equal(3, run.ApprovalSteps.Count);
    }

    [Fact]
    public async Task ApproveRunAsync_ShouldApproveLastStepAndClosePeriod()
    {
        // ARRANGE
        var service = CreateService();
        var runId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var run = new PayrollRun { 
            Id = runId, 
            Status = "pending_approval", 
            TotalNetPay = 5000m,
            PayrollPeriodId = periodId,
            TenantId = Guid.NewGuid().ToString()
        };
        run.ApprovalSteps.Add(new ApprovalFlow { Step = 1, Status = "pending", RequestType = "payroll" });
        
        _mockRepo.Setup(r => r.GetRunByIdAsync(runId)).ReturnsAsync(run);
        _mockRepo.Setup(r => r.GetPeriodByIdAsync(periodId)).ReturnsAsync(new PayrollPeriod { Id = periodId });
        _mockTenant.Setup(t => t.CurrentEmployeeId).Returns(Guid.NewGuid());

        // ACT
        await service.ApproveRunAsync(runId);

        // ASSERT
        Assert.Equal("approved", run.Status);
        Assert.All(run.ApprovalSteps, s => Assert.Equal("approved", s.Status));
        _mockAutoAccounting.Verify(a => a.GeneratePayrollEntryAsync(runId), Times.Once);
    }

    [Fact]
    public async Task GetPayrollDashboardAsync_ShouldReturnData()
    {
        // ARRANGE
        var mockDashRepo = new Mock<IDashboardRepository>();
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockCreditRepo = new Mock<ICreditRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var mockCashRepo = new Mock<ICashRegisterRepository>();

        mockDashRepo.Setup(r => r.GetPayrollCostByDepartmentAsync())
            .ReturnsAsync(new List<(string, decimal)> { ("IT", 5000m), ("HR", 3000m) });
        mockDashRepo.Setup(r => r.GetPayrollHistoryAsync(6))
            .ReturnsAsync(new List<(string, decimal)> { ("Jan", 8000m) });

        var dashboardService = new DashboardService(mockDashRepo.Object, mockSaleRepo.Object, 
            mockCreditRepo.Object, mockProductRepo.Object, mockCashRepo.Object);

        // ACT
        var result = await dashboardService.GetPayrollDashboardAsync();

        // ASSERT
        Assert.Equal(8000m, result.TotalLastPayroll);
        Assert.Equal(2, result.CostsByDepartment.Count);
        Assert.Single(result.History);
    }

    [Fact]
    public async Task GeneratePayrollAsync_ShouldCalculateHourlySalaryCorrectly()
    {
        // ARRANGE
        var service = CreateService();
        var periodId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        var period = new PayrollPeriod { Id = periodId, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 15) };
        var emp = new Employee { Id = empId, SalaryType = "hourly", Salary = 10m, IdentificationNumber = "123" };
        var attendance = new List<AttendanceRecord> { 
            new AttendanceRecord { EmployeeId = empId, TotalHours = 8m, Status = "present" },
            new AttendanceRecord { EmployeeId = empId, TotalHours = 8m, Status = "present" }
        };

        _mockRepo.Setup(r => r.GetPeriodByIdAsync(periodId)).ReturnsAsync(period);
        _mockTenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _mockCompanyRepo.Setup(c => c.GetByTenantIdAsync(It.IsAny<string>())).ReturnsAsync(new Company { Country = "NIC" });
        _mockTaxConfigRepo.Setup(t => t.GetByCountryCodeAsync("NIC")).ReturnsAsync(new CountryTaxConfig { InssEmployeeRate = 0.07m });
        _mockEmployeeRepo.Setup(e => e.GetFilteredAsync(null, "active", null, 1, 10000)).ReturnsAsync(new List<Employee> { emp });
        _mockEmployeeRepo.Setup(e => e.GetAttendanceInRangeAsync(empId, period.StartDate, period.EndDate)).ReturnsAsync(attendance);
        _mockRepo.Setup(r => r.GetActiveSalaryAsync(empId)).ReturnsAsync(new EmployeeSalary { BaseSalary = 10m }); // 10 per hour
        _mockOvertimeRepo.Setup(o => o.GetByPeriodAsync(periodId, It.IsAny<Guid>())).ReturnsAsync(new List<OvertimeRecord>());
        _mockCommissionRepo.Setup(c => c.GetByPeriodAsync(periodId, It.IsAny<Guid>())).ReturnsAsync(new List<CommissionRecord>());
        _mockBonusRepo.Setup(b => b.GetByPeriodAsync(periodId, It.IsAny<Guid>())).ReturnsAsync(new List<BonusRecord>());
        _mockLoanRepo.Setup(l => l.GetActiveLoansAsync(empId, It.IsAny<string>())).ReturnsAsync(new List<EmployeeLoan>());
        _mockAdvanceRepo.Setup(a => a.GetByEmployeeAsync(empId, It.IsAny<string>())).ReturnsAsync(new List<SalaryAdvance>());
        _mockGarnishmentRepo.Setup(g => g.GetActiveGarnishmentsAsync(empId, It.IsAny<string>())).ReturnsAsync(new List<WageGarnishment>());
        _mockEmployeeRepo.Setup(e => e.GetVacationsInRangeAsync(empId, period.StartDate, period.EndDate)).ReturnsAsync(new List<VacationRequest>());
        _mockConceptRepo.Setup(c => c.GetAllAsync(It.IsAny<string>())).ReturnsAsync(new List<PayrollConceptDefinition>());

        // ACT
        var run = await service.GeneratePayrollAsync(new GeneratePayrollRequest(periodId, "Test"));

        // ASSERT
        // 16 hours * 10 = 160 gross pay
        Assert.Equal(160m, run.Details[0].GrossPay);
    }

    [Fact]
    public async Task GeneratePayrollAsync_ShouldAdjustSalaryByAttendance()
    {
        // ARRANGE
        var service = CreateService();
        var periodId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        // 5 day period for simplicity
        var period = new PayrollPeriod { Id = periodId, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 5) };
        var emp = new Employee { Id = empId, SalaryType = "monthly", Salary = 1000m, IdentificationNumber = "123" };
        // Worked 3 out of 5 days
        var attendance = new List<AttendanceRecord> { 
            new AttendanceRecord { Date = new DateOnly(2026, 6, 1), Status = "present" },
            new AttendanceRecord { Date = new DateOnly(2026, 6, 2), Status = "present" },
            new AttendanceRecord { Date = new DateOnly(2026, 6, 3), Status = "present" }
        };

        _mockRepo.Setup(r => r.GetPeriodByIdAsync(periodId)).ReturnsAsync(period);
        _mockTenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _mockCompanyRepo.Setup(c => c.GetByTenantIdAsync(It.IsAny<string>())).ReturnsAsync(new Company { Country = "NIC" });
        _mockTaxConfigRepo.Setup(t => t.GetByCountryCodeAsync("NIC")).ReturnsAsync(new CountryTaxConfig { InssEmployeeRate = 0.07m });
        _mockEmployeeRepo.Setup(e => e.GetFilteredAsync(null, "active", null, 1, 10000)).ReturnsAsync(new List<Employee> { emp });
        _mockEmployeeRepo.Setup(e => e.GetAttendanceInRangeAsync(empId, period.StartDate, period.EndDate)).ReturnsAsync(attendance);
        _mockRepo.Setup(r => r.GetActiveSalaryAsync(empId)).ReturnsAsync(new EmployeeSalary { BaseSalary = 1000m });
        _mockOvertimeRepo.Setup(o => o.GetByPeriodAsync(periodId, It.IsAny<Guid>())).ReturnsAsync(new List<OvertimeRecord>());
        _mockCommissionRepo.Setup(c => c.GetByPeriodAsync(periodId, It.IsAny<Guid>())).ReturnsAsync(new List<CommissionRecord>());
        _mockBonusRepo.Setup(b => b.GetByPeriodAsync(periodId, It.IsAny<Guid>())).ReturnsAsync(new List<BonusRecord>());
        _mockLoanRepo.Setup(l => l.GetActiveLoansAsync(empId, It.IsAny<string>())).ReturnsAsync(new List<EmployeeLoan>());
        _mockAdvanceRepo.Setup(a => a.GetByEmployeeAsync(empId, It.IsAny<string>())).ReturnsAsync(new List<SalaryAdvance>());
        _mockGarnishmentRepo.Setup(g => g.GetActiveGarnishmentsAsync(empId, It.IsAny<string>())).ReturnsAsync(new List<WageGarnishment>());
        _mockEmployeeRepo.Setup(e => e.GetVacationsInRangeAsync(empId, period.StartDate, period.EndDate)).ReturnsAsync(new List<VacationRequest>());
        _mockConceptRepo.Setup(c => c.GetAllAsync(It.IsAny<string>())).ReturnsAsync(new List<PayrollConceptDefinition>());

        // ACT
        var run = await service.GeneratePayrollAsync(new GeneratePayrollRequest(periodId, "Test"));

        // ASSERT
        // (1000 / 5) * 3 = 600
        Assert.Equal(600m, run.Details[0].GrossPay);
    }

    [Fact]
    public async Task MarkAsPaidAsync_ShouldSimulatePaymentsBasedOnBankAccounts()
    {
        // ARRANGE
        var service = CreateService();
        var runId = Guid.NewGuid();
        var emp1Id = Guid.NewGuid();
        var emp2Id = Guid.NewGuid();
        
        var run = new PayrollRun { 
            Id = runId, Status = "approved", 
            Details = new List<PayrollDetail> {
                new PayrollDetail { EmployeeId = emp1Id, NetPay = 1000m },
                new PayrollDetail { EmployeeId = emp2Id, NetPay = 2000m }
            }
        };

        _mockRepo.Setup(r => r.GetRunByIdAsync(runId)).ReturnsAsync(run);
        // Emp1 has bank account, Emp2 doesn't
        _mockEmployeeRepo.Setup(e => e.GetBankAccountsAsync(emp1Id)).ReturnsAsync(new List<EmployeeBankAccount> { new EmployeeBankAccount { IsActive = true } });
        _mockEmployeeRepo.Setup(e => e.GetBankAccountsAsync(emp2Id)).ReturnsAsync(new List<EmployeeBankAccount>());

        // ACT
        await service.MarkAsPaidAsync(runId);

        // ASSERT
        Assert.Equal("paid", run.Status);
        Assert.Equal("paid", run.Details.First(d => d.EmployeeId == emp1Id).PaymentStatus);
        Assert.Equal("failed", run.Details.First(d => d.EmployeeId == emp2Id).PaymentStatus);
    }
}
