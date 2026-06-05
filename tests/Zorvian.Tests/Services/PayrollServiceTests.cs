using Moq;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class PayrollServiceTests
{
    private readonly Mock<IPayrollRepository> _repo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IWebhookService> _webhook = new();
    private readonly Mock<IAchExportService> _ach = new();
    private readonly Mock<IAutoAccountingService> _autoAccounting = new();
    private readonly Mock<ICountryTaxConfigRepository> _taxConfigRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<IOvertimeRecordRepository> _overtimeRepo = new();
    private readonly Mock<ICommissionRecordRepository> _commissionRepo = new();
    private readonly Mock<IBonusRecordRepository> _bonusRepo = new();
    private readonly Mock<IPayrollConceptRepository> _conceptRepo = new();
    private readonly Mock<IEmployeeLoanRepository> _loanRepo = new();
    private readonly Mock<ISalaryAdvanceRepository> _advanceRepo = new();
    private readonly Mock<IWageGarnishmentRepository> _garnishmentRepo = new();
    private readonly Mock<IBenefitProvisionRepository> _benefitProvisionRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly Mock<IBankTransferService> _bankTransferService = new();
    private readonly Mock<ISickLeaveRepository> _sickLeaveRepo = new();
    private readonly PayrollService _sut;

    public PayrollServiceTests()
    {
        var mockStrategy = new Mock<IPayrollCalculationStrategy>();
        var factory = new PayrollCalculationFactory(new List<IPayrollCalculationStrategy> { mockStrategy.Object });

        _sut = new PayrollService(_repo.Object, _employeeRepo.Object, _tenant.Object, _webhook.Object, _ach.Object, 
            _autoAccounting.Object, _taxConfigRepo.Object, _companyRepo.Object, 
            _overtimeRepo.Object, _commissionRepo.Object, _bonusRepo.Object, _conceptRepo.Object,
            _loanRepo.Object, _advanceRepo.Object, _garnishmentRepo.Object, _benefitProvisionRepo.Object,
            _auditRepo.Object, factory, _bankTransferService.Object, _sickLeaveRepo.Object);
    }

    [Fact]
    public async Task ExportAchFileAsync_Should_Return_Result_When_Run_Is_Approved()
    {
        var runId = Guid.NewGuid();
        var run = new PayrollRun { Id = runId, Status = "approved" };
        _repo.Setup(r => r.GetRunByIdAsync(runId)).ReturnsAsync(run);

        var expectedContent = new byte[] { 1, 2, 3 };
        _ach.Setup(a => a.GenerateAchFileAsync(runId)).ReturnsAsync(expectedContent);
        _ach.SetupGet(a => a.FileName).Returns("test_ach.csv");

        var result = await _sut.ExportAchFileAsync(runId);

        Assert.NotNull(result);
        Assert.Equal(expectedContent, result.Content);
        Assert.Equal("test_ach.csv", result.FileName);
    }

    [Fact]
    public async Task ExportAchFileAsync_Should_Return_Null_When_Run_Not_Approved()
    {
        var runId = Guid.NewGuid();
        var run = new PayrollRun { Id = runId, Status = "draft" };
        _repo.Setup(r => r.GetRunByIdAsync(runId)).ReturnsAsync(run);

        var result = await _sut.ExportAchFileAsync(runId);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExportAchFileAsync_Should_Return_Null_When_Run_Not_Found()
    {
        var runId = Guid.NewGuid();
        _repo.Setup(r => r.GetRunByIdAsync(runId)).ReturnsAsync((PayrollRun?)null);

        var result = await _sut.ExportAchFileAsync(runId);

        Assert.Null(result);
    }
}
