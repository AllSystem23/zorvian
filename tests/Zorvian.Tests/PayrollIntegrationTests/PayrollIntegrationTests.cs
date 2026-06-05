using Moq;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.PayrollIntegrationTests;

public class PayrollIntegrationTests
{
    [Fact]
    public async Task GeneratePayroll_WithVariableConcepts_ShouldIncludeThemInCalculations()
    {
        // ARRANGE
        var mockRepo = new Mock<IPayrollRepository>();
        var mockEmployeeRepo = new Mock<IEmployeeRepository>();
        var mockTenant = new Mock<ITenantContext>();
        var mockWebhook = new Mock<IWebhookService>();
        var mockAch = new Mock<IAchExportService>();
        var mockAutoAccounting = new Mock<IAutoAccountingService>();
        var mockTaxConfigRepo = new Mock<ICountryTaxConfigRepository>();
        var mockCompanyRepo = new Mock<ICompanyRepository>();
        
        var mockOvertimeRepo = new Mock<IOvertimeRecordRepository>();
        var mockCommissionRepo = new Mock<ICommissionRecordRepository>();
        var mockBonusRepo = new Mock<IBonusRecordRepository>();

        var periodId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        var period = new PayrollPeriod { Id = periodId, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 30) };
        
        mockTenant.Setup(t => t.TenantId).Returns(companyId.ToString());
        mockRepo.Setup(r => r.GetPeriodByIdAsync(periodId)).ReturnsAsync(period);
        mockCompanyRepo.Setup(r => r.GetByTenantIdAsync(companyId.ToString())).ReturnsAsync(new Company { Id = companyId, Country = "NIC" });
        mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC")).ReturnsAsync(new CountryTaxConfig { InssEmployeeRate = 0.07m, InssEmployeeMax = 1500m, InssEmployerRate = 0.215m });

        var emp = new Employee { Id = empId, Salary = 1000m, SalaryType = "monthly" };
        mockEmployeeRepo.Setup(r => r.GetFilteredAsync(null, "active", null, 1, 10000)).ReturnsAsync(new List<Employee> { emp });
        
        // Mock full attendance for the month to get full base salary
        var fullAttendance = Enumerable.Range(1, 30).Select(i => new AttendanceRecord { Date = new DateOnly(2026, 6, i), Status = "present" }).ToList();
        mockEmployeeRepo.Setup(r => r.GetAttendanceInRangeAsync(empId, period.StartDate, period.EndDate)).ReturnsAsync(fullAttendance);
        
        mockEmployeeRepo.Setup(r => r.GetVacationsInRangeAsync(empId, period.StartDate, period.EndDate)).ReturnsAsync(new List<VacationRequest>());
mockOvertimeRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId)).ReturnsAsync(new List<OvertimeRecord> { new() { EmployeeId = empId, Amount = 100m } });
mockCommissionRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId)).ReturnsAsync(new List<CommissionRecord> { new() { EmployeeId = empId, Amount = 200m } });
mockBonusRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId)).ReturnsAsync(new List<BonusRecord> { new() { EmployeeId = empId, Amount = 50m } });

// Mock payroll save (AddRunAsync, AddDetailsAsync) to avoid NullReferenceException if any
mockRepo.Setup(r => r.AddRunAsync(It.IsAny<PayrollRun>())).Verifiable();
mockRepo.Setup(r => r.AddDetailsAsync(It.IsAny<List<PayrollDetail>>())).Verifiable();
mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

var mockConceptRepo = new Mock<IPayrollConceptRepository>();
mockConceptRepo.Setup(r => r.GetAllAsync(It.IsAny<string>())).ReturnsAsync(new List<PayrollConceptDefinition>());
var mockLoanRepo = new Mock<IEmployeeLoanRepository>();
mockLoanRepo.Setup(r => r.GetActiveLoansAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(new List<EmployeeLoan>());
var mockAdvanceRepo = new Mock<ISalaryAdvanceRepository>();
mockAdvanceRepo.Setup(r => r.GetByEmployeeAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(new List<SalaryAdvance>());
var mockGarnishmentRepo = new Mock<IWageGarnishmentRepository>();
mockGarnishmentRepo.Setup(r => r.GetActiveGarnishmentsAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(new List<WageGarnishment>());
var mockBenefitProvisionRepo = new Mock<IBenefitProvisionRepository>();
mockBenefitProvisionRepo.Setup(r => r.AddAsync(It.IsAny<BenefitProvision>())).Verifiable();
mockBenefitProvisionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

var mockStrategy = new Mock<IPayrollCalculationStrategy>();
mockStrategy.Setup(s => s.CountryCode).Returns("NIC");
var factory = new PayrollCalculationFactory(new List<IPayrollCalculationStrategy> { mockStrategy.Object });

var mockAuditRepo = new Mock<IAuditLogRepository>();
var mockBankTransfer = new Mock<IBankTransferService>();
var mockSickLeaveRepo = new Mock<ISickLeaveRepository>();

var payrollService = new PayrollService(
    mockRepo.Object, mockEmployeeRepo.Object, mockTenant.Object, mockWebhook.Object, 
    mockAch.Object, mockAutoAccounting.Object, mockTaxConfigRepo.Object, mockCompanyRepo.Object,
    mockOvertimeRepo.Object, mockCommissionRepo.Object, mockBonusRepo.Object, 
    mockConceptRepo.Object, mockLoanRepo.Object, mockAdvanceRepo.Object, mockGarnishmentRepo.Object,
    mockBenefitProvisionRepo.Object, mockAuditRepo.Object, factory, mockBankTransfer.Object, mockSickLeaveRepo.Object);

// ACT
var request = new GeneratePayrollRequest(periodId, "Nómina de prueba");
var run = await payrollService.GeneratePayrollAsync(request);

// ASSERT
// Expected gross: Base(1000) + Overtime(100) + Commission(200) + Bonus(50) = 1350
Assert.Equal(1350m, run.TotalSalaries);
}
}
