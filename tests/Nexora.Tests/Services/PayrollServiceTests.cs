using Moq;
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
    private readonly PayrollService _sut;

    public PayrollServiceTests()
    {
        _sut = new PayrollService(_repo.Object, _employeeRepo.Object, _tenant.Object, _webhook.Object);
    }

    [Fact]
    public async Task ExportAchFileAsync_Should_Return_Csv_Content_When_Run_Is_Approved()
    {
        var runId = Guid.NewGuid();
        var run = new PayrollRun
        {
            Id = runId,
            Status = "approved",
            PayrollPeriod = new PayrollPeriod { Name = "Enero 2026" },
            Details = new List<PayrollDetail>
            {
                new() 
                { 
                    NetPay = 1000, 
                    Employee = new Employee 
                    { 
                        FirstName = "Carlos", 
                        LastName = "Mendoza", 
                        BankAccountNumber = "12345", 
                        BankName = "BAC", 
                        BankAccountType = "Ahorro" 
                    } 
                }
            }
        };
        _repo.Setup(r => r.GetRunByIdAsync(runId)).ReturnsAsync(run);

        var result = await _sut.ExportAchFileAsync(runId);

        Assert.NotNull(result);
        var csv = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("Carlos Mendoza", csv);
        Assert.Contains("12345", csv);
        Assert.Contains("1000", csv);
        Assert.Equal("ACH_Nomina_Enero 2026.csv", result.Value.FileName);
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
}
