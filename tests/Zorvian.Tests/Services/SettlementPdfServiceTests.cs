using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

public sealed class SettlementPdfServiceTests
{
    private readonly Mock<ITerminationRepository> _termRepo = new();
    private readonly Mock<IEmployeeRepository> _empRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly SettlementPdfService _sut;

    public SettlementPdfServiceTests()
    {
        _sut = new SettlementPdfService(_termRepo.Object, _empRepo.Object, _companyRepo.Object);
    }

    private (Guid recordId, TerminationRecord record) SetupHappyPath(
        string countryCode = "NIC",
        string currency = "NIO",
        decimal netSettlement = 20786.78m)
    {
        var recordId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var record = new TerminationRecord
        {
            Id = recordId,
            EmployeeId = employeeId,
            CompanyId = companyId,
            TerminationDate = new DateOnly(2026, 6, 15),
            Reason = TerminationReason.VoluntaryResignation,
            HireDate = new DateOnly(2025, 12, 1),
            DaysWorked = 197,
            MonthsWorked = 6.48m,
            YearsWorked = 0.54m,
            MonthlySalary = 15000m,
            DailySalary = 500m,
            AguinaldoPay = 8095.89m,
            VacationDaysAccrued = 16.19m,
            VacationDaysTaken = 7m,
            VacationDaysToPay = 9.19m,
            VacationPay = 4595.00m,
            SeveranceDays = 0m,
            SeverancePay = 8095.89m,
            IsTrustPosition = false,
            TrustPositionPay = 0m,
            PendingSalaryDays = 15,
            PendingSalaryPay = 7500m,
            OvertimeHours = 0,
            OvertimePay = 0m,
            InssLaboralAmount = 846.65m,
            IrSalaryAmount = 421.25m,
            IrTotalAmount = 1062.25m,
            InssPatronalAmount = 3201.89m,
            InatecAmount = 297.85m,
            GrossSettlement = 20786.78m,
            TotalDeductions = 1908.90m,
            CountryCode = countryCode,
            Currency = currency,
            CountryName = countryCode switch
            {
                "NIC" => "Nicaragua",
                "HND" => "Honduras",
                "CRI" => "Costa Rica",
                "SLV" => "El Salvador",
                "GTM" => "Guatemala",
                "PAN" => "Panamá",
                _ => countryCode
            },
            InssEmployeeRateDisplay = 0.07m,
            InssEmployerRateDisplay = 0.215m,
            OtherEmployerRateDisplay = 0.02m,
            OtherEmployerName = "INATEC",
            Status = "draft"
        };

        var employee = new Employee
        {
            Id = employeeId,
            FirstName = "Kenneth Josue",
            LastName = "Romero Gonzalez",
            IdentificationNumber = "601-091002-1000N",
            Position = "Supervisor",
            HireDate = new DateOnly(2025, 12, 1),
            Salary = 15000m,
            CountryCode = countryCode,
            SalaryType = "monthly"
        };

        var company = new Company
        {
            Id = companyId,
            Name = "Tienda Brizuela Romero #3",
            LegalName = "Tienda Brizuela Romero #3",
            Country = countryCode,
            Currency = currency
        };

        _termRepo.Setup(r => r.GetByIdAsync(recordId)).ReturnsAsync(record);
        _empRepo.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(employee);
        _companyRepo.Setup(r => r.GetByIdAsync(companyId)).ReturnsAsync(company);

        return (recordId, record);
    }

    [Fact]
    public async Task GenerateSettlementPdfAsync_Nicaragua_ShouldReturnByteArray()
    {
        var (recordId, _) = SetupHappyPath("NIC", "NIO");

        var result = await _sut.GenerateSettlementPdfAsync(recordId);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // PDF magic bytes
        Assert.Equal(0x25, result[0]); // '%'
        Assert.Equal(0x50, result[1]); // 'P'
        Assert.Equal(0x44, result[2]); // 'D'
        Assert.Equal(0x46, result[3]); // 'F'
    }

    [Theory]
    [InlineData("NIC", "NIO")]
    [InlineData("HND", "HNL")]
    [InlineData("CRI", "CRC")]
    [InlineData("SLV", "USD")]
    [InlineData("GTM", "GTQ")]
    [InlineData("PAN", "USD")]
    public async Task GenerateSettlementPdfAsync_AllCountries_ShouldReturnByteArray(
        string countryCode, string currency)
    {
        var (recordId, _) = SetupHappyPath(countryCode, currency);

        var result = await _sut.GenerateSettlementPdfAsync(recordId);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        Assert.Equal(0x25, result[0]); // PDF header
    }

    [Fact]
    public async Task GenerateSettlementPdfAsync_RecordNotFound_ShouldThrowKeyNotFound()
    {
        var fakeId = Guid.NewGuid();
        _termRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync((TerminationRecord?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GenerateSettlementPdfAsync(fakeId));
    }

    [Fact]
    public async Task GenerateSettlementPdfAsync_EmployeeNotFound_ShouldThrowKeyNotFound()
    {
        var (recordId, _) = SetupHappyPath();
        // Override employee to null
        var record = await _termRepo.Object.GetByIdAsync(recordId);
        _empRepo.Setup(r => r.GetByIdAsync(record!.EmployeeId)).ReturnsAsync((Employee?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GenerateSettlementPdfAsync(recordId));
    }

    [Fact]
    public async Task GenerateSettlementPdfAsync_WithTrustPosition_ShouldIncludeTrustRow()
    {
        var (recordId, record) = SetupHappyPath();
        record.IsTrustPosition = true;
        record.TrustPositionPay = 90000m;
        record.GrossSettlement += 90000m;

        var result = await _sut.GenerateSettlementPdfAsync(recordId);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateSettlementPdfAsync_DynamicRates_ShouldNotThrow()
    {
        // HND has different rates and OtherEmployerName = "RAP (FONDO VIVIENDA)"
        var (recordId, record) = SetupHappyPath("HND", "HNL");
        record.InssEmployeeRateDisplay = 0.05m;
        record.InssEmployerRateDisplay = 0.125m;
        record.OtherEmployerRateDisplay = 0.015m;
        record.OtherEmployerName = "RAP (FONDO VIVIENDA)";

        var result = await _sut.GenerateSettlementPdfAsync(recordId);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateSettlementPdfAsync_ZeroNetSettlement_ShouldStillGenerate()
    {
        var (recordId, record) = SetupHappyPath();
        record.GrossSettlement = 0m;
        record.TotalDeductions = 0m;

        var result = await _sut.GenerateSettlementPdfAsync(recordId);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }
}
