using Moq;
using Nexora.Application.DTOs.Payroll;
using Nexora.Application.Interfaces;
using Nexora.Application.Services;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Tests.Services;

public sealed class PayrollServiceTests
{
    private readonly Mock<IPayrollRepository> _repo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IWebhookService> _webhook = new();
    private readonly Mock<IAchExportService> _ach = new();
    private readonly PayrollService _sut;

    public PayrollServiceTests()
    {
        _sut = new PayrollService(_repo.Object, _employeeRepo.Object, _tenant.Object, _webhook.Object, _ach.Object);
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
