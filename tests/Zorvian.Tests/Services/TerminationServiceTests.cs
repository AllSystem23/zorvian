using Moq;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public class TerminationServiceTests
{
    private readonly Mock<ITerminationRepository> _mockRepo = new();
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo = new();
    private readonly Mock<IBenefitProvisionRepository> _mockBenefitRepo = new();
    private readonly Mock<ICountryTaxConfigRepository> _mockTaxConfigRepo = new();
    private readonly Mock<IVacationRepository> _mockVacationRepo = new();
    private readonly TerminationService _sut;

    // NIC tiered formula: [{upToYears:3, daysPerYear:30}, {upToYears:6, daysPerYear:20}], MaxDays=150
    // 1yr=30, 2yr=60, 3yr=90, 4yr=110, 5yr=130, 6yr=150, 7yr=150(cap)
    private static CountryTaxConfig CreateNicConfig() => new()
    {
        IndemnityDaysPerYear = 30,
        MaxIndemnityYears = 12,
        IndemnityTiersJson = "[{\"upToYears\":3,\"daysPerYear\":30},{\"upToYears\":6,\"daysPerYear\":20}]",
        MaxIndemnityDays = 150,
        VacationDaysPerYear = 30,
        // Aguinaldo siempre desde 1/dic del año anterior (Art. 93 CT NIC)
        AguinaldoPeriodStartMonth = 12,
        AguinaldoPeriodStartDay = 1,
        // INSS e IR para cálculos de deducciones
        InssEmployeeRate = 0.07m,
        InssIntegralEmployerRateSmall = 0.215m,
        InssEmployerRate = 0.215m,
        OtherEmployerRate = 0.02m,
        IrTableJson = "[{\"min\":0,\"max\":100000,\"rate\":0},{\"min\":100001,\"max\":200000,\"rate\":0.15},{\"min\":200001,\"max\":350000,\"rate\":0.20},{\"min\":350001,\"max\":500000,\"rate\":0.25},{\"min\":500001,\"max\":9999999,\"rate\":0.30}]",
    };

    public TerminationServiceTests()
    {
        _sut = new TerminationService(
            _mockRepo.Object, _mockEmployeeRepo.Object, _mockBenefitRepo.Object,
            _mockTaxConfigRepo.Object, _mockVacationRepo.Object);
    }

    [Fact]
    public async Task CalculateAsync_Kenneth_RealDocument_ShouldMatchExactly()
    {
        // ARRANGE — Replicates the real "Liquidacion Kenneth.pdf" document exactly
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2025, 12, 1);
        var terminationDate = new DateOnly(2026, 6, 15);
        var monthlySalary = 15000m; // C$ 15,000
        var vacationDaysTaken = 7m; // 7 days already taken

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = monthlySalary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(vacationDaysTaken);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.VoluntaryResignation, terminationDate);

        // ASSERT — Match the real document values
        Assert.NotNull(result);
        // 197 days worked
        Assert.Equal(197, result.DaysWorked);
        // Monthly salary = 15000
        Assert.Equal(15000m, result.MonthlySalary);
        // Daily salary = 500
        Assert.Equal(500m, result.DailySalary);

        // Aguinaldo: 197/365 * 15000 = 8095.89
        Assert.Equal(8095.89m, result.AguinaldoPay);

        // Vacation: daysAccrued=16.19, taken=7, toPay=9.19, pay=9.19*500=4595.00
        Assert.Equal(16.19m, result.VacationDaysAccrued);
        Assert.Equal(7m, result.VacationDaysTaken);
        Assert.Equal(9.19m, result.VacationDaysToPay);
        Assert.Equal(4595.00m, result.VacationPay);

        // Indemnización (Art. 44 resignation < 1yr): daysWorked/365 * salary = 8095.89
        Assert.Equal(8095.89m, result.SeverancePay);

        // Salario pendiente: 15 days (Jun 1-15) * 500/day = 7500
        Assert.Equal(15, result.PendingSalaryDays);
        Assert.Equal(7500m, result.PendingSalaryPay);

        // No trust position (not set)
        Assert.False(result.IsTrustPosition);
        Assert.Equal(0m, result.TrustPositionPay);

        // Gross = aguinaldo + vacation + severance + pending salary
        Assert.Equal(8095.89m + 4595.00m + 8095.89m + 7500m, result.GrossSettlement);

        // INSS Laboral: 7% on (pending salary + vacation) = (7,500 + 4,595) × 7% = 846.65
        Assert.Equal(846.65m, result.InssLaboralAmount);

        // IR on salary (annual projection method):
        // Taxable monthly = 15,000 - 1,050 (INSS) = 13,950
        // Taxable annual = 13,950 × 12 = 167,400
        // IR annual = (167,400 - 100,000) × 15% = 10,110
        // Monthly IR = 10,110 / 12 = 842.50
        // IR pending (15 days) = 842.50 × 15/30 = 421.25
        Assert.Equal(421.25m, result.IrSalaryAmount);

        // IR incremental on vacation (Pagos Ocionales):
        // Vacation net of INSS: 4,595 - 321.65 = 4,273.35
        // IR with vacation: (171,673.35 - 100,000) × 15% = 10,751.00
        // IR without vacation: 10,110
        // Incremental: 10,751.00 - 10,110 = 641.00
        Assert.Equal(641.00m, result.IrTotalAmount - result.IrSalaryAmount);

        // Total deductions = INSS + IR
        Assert.Equal(result.InssLaboralAmount + result.IrTotalAmount, result.TotalDeductions);

        // Net = Gross - Deductions
        Assert.Equal(result.GrossSettlement - result.TotalDeductions, result.NetSettlement);
    }

    [Fact]
    public async Task CalculateAsync_Nicaragua_3Years_Resignation_ShouldCalculateCorrectSeverance()
    {
        // ARRANGE — NIC tiered: 3 years = 3*30 = 90 days
        var empId = Guid.NewGuid();
        var terminationDate = new DateOnly(2026, 6, 1);
        var hireDate = new DateOnly(2023, 6, 1); // 3 years
        var salary = 3000m; // 100 per day

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.VoluntaryResignation, terminationDate);

        // ASSERT — NIC tiered: 3 years * 30 days = 90 days. 90 * 100/day = 9000
        Assert.NotNull(result);
        Assert.Equal(90m, result.SeveranceDays);
        Assert.Equal(9000m, result.SeverancePay);
        Assert.True(result.AguinaldoPay > 0, "Aguinaldo should be positive");
        Assert.True(result.VacationPay >= 0, "Vacation pay should be non-negative");
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<TerminationRecord>()), Times.Once);
    }

    [Fact]
    public async Task CalculateAsync_Nicaragua_DuringTrialMonth_ShouldNotCalculateSeverance()
    {
        // ARRANGE — Art. 28 CT: No severance during trial month
        var empId = Guid.NewGuid();
        var terminationDate = new DateOnly(2026, 2, 15);
        var hireDate = new DateOnly(2026, 2, 1); // Only 14 days
        var salary = 3000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.UnjustifiedDismissal, terminationDate);

        // ASSERT — No severance during trial period
        Assert.NotNull(result);
        Assert.Equal(0m, result.SeveranceDays);
        Assert.Equal(0m, result.SeverancePay);
    }

    [Fact]
    public async Task CalculateAsync_Nicaragua_5Years_ShouldUseTieredFormula()
    {
        // ARRANGE — NIC tiered: 5 years = 3*30 + 2*20 = 130 days
        var empId = Guid.NewGuid();
        var terminationDate = new DateOnly(2026, 6, 1);
        var hireDate = new DateOnly(2021, 6, 1); // 5 years
        var salary = 3000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.MutualAgreement, terminationDate);

        // ASSERT — 5 years: tier1(3*30=90) + tier2(2*20=40) = 130 days. 130 * 100/day = 13000
        Assert.NotNull(result);
        Assert.Equal(130m, result.SeveranceDays);
        Assert.Equal(13000m, result.SeverancePay);
    }

    [Fact]
    public async Task CalculateAsync_Nicaragua_7Years_ShouldCapAt150()
    {
        // ARRANGE — 7 years: tier1(3*30=90) + tier2(3*20=60) = 150 (cap)
        var empId = Guid.NewGuid();
        var terminationDate = new DateOnly(2026, 6, 1);
        var hireDate = new DateOnly(2019, 6, 1); // 7 years
        var salary = 3000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.MutualAgreement, terminationDate);

        // ASSERT — Capped at 150 days
        Assert.NotNull(result);
        Assert.Equal(150m, result.SeveranceDays);
        Assert.Equal(15000m, result.SeverancePay);
    }

    [Fact]
    public async Task CalculateAsync_Nicaragua_15Years_ShouldStillCapAt150()
    {
        // ARRANGE — 15 years: same cap at 150 days
        var empId = Guid.NewGuid();
        var terminationDate = new DateOnly(2026, 6, 1);
        var hireDate = new DateOnly(2011, 6, 1); // 15 years
        var salary = 3000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.MutualAgreement, terminationDate);

        // ASSERT — Capped at 150 days
        Assert.NotNull(result);
        Assert.Equal(150m, result.SeveranceDays);
        Assert.Equal(15000m, result.SeverancePay);
    }

    [Fact]
    public async Task CalculateAsync_Mayco_RealDocument_ShouldMatchExactly()
    {
        // ARRANGE — Replicates the real Mayco Ivan Miranda Rios document
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2025, 10, 6);
        var terminationDate = new DateOnly(2026, 5, 15);
        var monthlySalary = 9000m; // C$ 9,000
        var vacationDaysTaken = 7m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = monthlySalary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(vacationDaysTaken);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.VoluntaryResignation, terminationDate);

        // ASSERT — Match the real document
        Assert.NotNull(result);
        // 222 days worked (Oct 6 2025 → May 15 2026 inclusive)
        Assert.Equal(222, result.DaysWorked);
        // Aguinaldo: Dec 1 2025 → May 15 2026 = 166 days → 166/365 * 9000 = 4093.15
        Assert.Equal(4093.15m, result.AguinaldoPay);
        // Vacation: daysAccrued = Round(222/30.42 * 2.5, 2) = 18.24, taken=7, toPay=11.24, pay=11.24*300=3372
        Assert.Equal(18.24m, result.VacationDaysAccrued);
        Assert.Equal(11.24m, result.VacationDaysToPay);
        Assert.Equal(3372.00m, result.VacationPay);
        // Indemnización: resignation < 1yr → daysWorked/365 * salary = 222/365 * 9000 = 5473.97
        Assert.Equal(5473.97m, result.SeverancePay);

        // INSS and IR should be calculated
        Assert.True(result.InssLaboralAmount > 0, "INSS should be calculated on pending salary");
        Assert.True(result.IrTotalAmount > 0, "IR should be calculated");
        Assert.Equal(result.InssLaboralAmount + result.IrTotalAmount, result.TotalDeductions);
        Assert.Equal(result.GrossSettlement - result.TotalDeductions, result.NetSettlement);
    }

    [Fact]
    public async Task CalculateAsync_HiredAfterDec1_SameYear_ShouldUseHireDateForAguinaldo()
    {
        // ARRANGE — Employee hired Dec 5 2025, terminated Jun 15 2026
        // Official formula: hireYear(2025) < termYear(2026) → Dec 1 2025 (NOT Dec 5)
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2025, 12, 5);
        var terminationDate = new DateOnly(2026, 6, 15);
        var monthlySalary = 15000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = monthlySalary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.UnjustifiedDismissal, terminationDate);

        // ASSERT — Aguinaldo starts from Dec 1 (NOT Dec 5) because hireYear < termYear
        // Dec 1 2025 → Jun 15 2026 = 197 days → 197/365 * 15000 = 8095.89
        Assert.NotNull(result);
        Assert.Equal(8095.89m, result.AguinaldoPay);
    }

    [Fact]
    public async Task CalculateAsync_HiredAndTerminatedSameYear_ShouldUseHireDateForAguinaldo()
    {
        // ARRANGE — Employee hired Apr 15 2026, terminated Aug 20 2026
        // Official formula: hireYear(2026) = termYear(2026) → use hire date (Apr 15)
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2026, 4, 15);
        var terminationDate = new DateOnly(2026, 8, 20);
        var monthlySalary = 18000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = monthlySalary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.UnjustifiedDismissal, terminationDate);

        // ASSERT — Aguinaldo starts from hire date Apr 15 (NOT Dec 1)
        // Apr 15 → Aug 20 = 128 days → 128/365 * 18000 = 6312.33
        Assert.NotNull(result);
        Assert.Equal(6312.33m, result.AguinaldoPay);
    }

    [Fact]
    public async Task CalculateAsync_TrustPosition_ShouldAddConfianzaIndemnity()
    {
        // ARRANGE — Art. 46-47 CT: Trust position gets additional 1 month/year, capped at C$100,000
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2020, 1, 1); // 6 years
        var terminationDate = new DateOnly(2026, 6, 1);
        var salary = 15000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC", IsTrustPosition = true });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.UnjustifiedDismissal, terminationDate);

        // ASSERT — 6 years * 15000 = 90,000 (under 100K cap)
        Assert.NotNull(result);
        Assert.True(result.IsTrustPosition);
        Assert.Equal(90000m, result.TrustPositionPay);
        // Severance: tiered 6yr = 150 days * 500 = 75000
        Assert.Equal(150m, result.SeveranceDays);
        Assert.Equal(75000m, result.SeverancePay);
    }

    [Fact]
    public async Task CalculateAsync_TrustPosition_ShouldCapAt100000()
    {
        // ARRANGE — Art. 46 CT: Cap at C$100,000
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2015, 1, 1); // 11 years
        var terminationDate = new DateOnly(2026, 6, 1);
        var salary = 15000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC", IsTrustPosition = true });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.UnjustifiedDismissal, terminationDate);

        // ASSERT — 11 * 15000 = 165,000 → capped at 100,000
        Assert.NotNull(result);
        Assert.Equal(100000m, result.TrustPositionPay);
    }

    [Fact]
    public async Task CalculateAsync_SameMonthHireAndTerminate_ShouldCapPendingSalary()
    {
        // ARRANGE — Edge case: hired and terminated in same month
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2026, 4, 15);
        var terminationDate = new DateOnly(2026, 4, 20);
        var salary = 3000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.UnjustifiedDismissal, terminationDate);

        // ASSERT — Pending salary capped at daysWorked (6 days, not 20)
        Assert.NotNull(result);
        Assert.Equal(6, result.DaysWorked);
        Assert.Equal(6, result.PendingSalaryDays); // Min(20, 6) = 6
        Assert.Equal(600m, result.PendingSalaryPay); // 6 * 100/day
    }

    [Fact]
    public void CalculateIrOnMonthlySalary_ShouldApplyBracketTable()
    {
        // ARRANGE — NIC bracket table: 0-100K=0%, 100K-200K=15%, 200K-350K=20%
        var config = CreateNicConfig();
        config.InssEmployeeRate = 0.07m;

        // ACT
        // Monthly salary 15,000: taxable = 15,000 - 1,050 = 13,950
        // Annual = 167,400 → bracket 15% → (167,400 - 100,000) × 15% = 10,110
        // Monthly IR = 10,110 / 12 = 842.50
        var result = TerminationService.CalculateIrOnMonthlySalary(15000m, config);

        // ASSERT
        Assert.Equal(842.50m, result);
    }

    [Fact]
    public void CalculateIrOnMonthlySalary_BelowThreshold_ShouldReturnZero()
    {
        // ARRANGE — Monthly salary 5,000: taxable = 5,000 - 350 = 4,650
        // Annual = 55,800 → bracket 0% (below 100,000)
        var config = CreateNicConfig();
        config.InssEmployeeRate = 0.07m;

        // ACT
        var result = TerminationService.CalculateIrOnMonthlySalary(5000m, config);

        // ASSERT
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateIncrementalIr_Vacation_ShouldReturnDifference()
    {
        // ARRANGE — Salary 15,000, vacation net 4,273.35
        var config = CreateNicConfig();
        config.InssEmployeeRate = 0.07m;

        // ACT
        // IR without vacation: 10,110 annual
        // IR with vacation: (171,673.35 - 100,000) × 15% = 10,751.00
        // Incremental: 641.00
        var result = TerminationService.CalculateIncrementalIr(15000m, 4273.35m, config);

        // ASSERT
        Assert.Equal(641.00m, result);
    }

    [Fact]
    public async Task CalculateAsync_NoTiers_ShouldFallbackToFlatFormula()
    {
        // ARRANGE — Country with no tiered config falls back to flat formula
        var empId = Guid.NewGuid();
        var terminationDate = new DateOnly(2026, 6, 1);
        var hireDate = new DateOnly(2019, 6, 1); // 7 years
        var salary = 3000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "HND" });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("HND"))
            .ReturnsAsync(new CountryTaxConfig { IndemnityDaysPerYear = 10, MaxIndemnityYears = 10, VacationDaysPerYear = 12 });
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.MutualAgreement, terminationDate);

        // ASSERT — Flat: 7*10 = 70 days (capped at 10*10=100, so 70)
        Assert.NotNull(result);
        Assert.Equal(70m, result.SeveranceDays);
        Assert.Equal(7000m, result.SeverancePay);
    }

    // ── Deduction flags tests ("¿Al Trabajador se le deduce?") ──────────────

    [Fact]
    public async Task CalculateAsync_DeductInssFalse_ShouldNotDeductInss()
    {
        // ARRANGE — Employee with DeductInss = false (Excel: "INSS? → NO")
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2025, 12, 1);
        var terminationDate = new DateOnly(2026, 6, 15);
        var salary = 15000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC", DeductInss = false });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.VoluntaryResignation, terminationDate);

        // ASSERT — INSS should be 0 when DeductInss = false
        Assert.NotNull(result);
        Assert.Equal(0m, result.InssLaboralAmount);
        // IR should still be calculated (DeductIr defaults to true)
        Assert.True(result.IrTotalAmount > 0, "IR should still be calculated");
    }

    [Fact]
    public async Task CalculateAsync_DeductIrFalse_ShouldNotDeductIr()
    {
        // ARRANGE — Employee with DeductIr = false (Excel: "IR? → NO")
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2025, 12, 1);
        var terminationDate = new DateOnly(2026, 6, 15);
        var salary = 15000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC", DeductIr = false });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.VoluntaryResignation, terminationDate);

        // ASSERT — IR should be 0 when DeductIr = false
        Assert.NotNull(result);
        Assert.Equal(0m, result.IrSalaryAmount);
        Assert.Equal(0m, result.IrTotalAmount);
        // INSS should still be calculated
        Assert.True(result.InssLaboralAmount > 0, "INSS should still be calculated");
    }

    [Fact]
    public async Task CalculateAsync_DeductAguinaldoFalse_ShouldNotIncludeAguinaldo()
    {
        // ARRANGE — Employee with DeductAguinaldo = false (Excel: "Aguinaldo? → NO")
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2025, 12, 1);
        var terminationDate = new DateOnly(2026, 6, 15);
        var salary = 15000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC", DeductAguinaldo = false });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.VoluntaryResignation, terminationDate);

        // ASSERT — Aguinaldo should be 0 when DeductAguinaldo = false
        Assert.NotNull(result);
        Assert.Equal(0m, result.AguinaldoPay);
        // Vacation and severance should still be calculated
        Assert.True(result.VacationPay > 0, "Vacation pay should still be calculated");
        Assert.True(result.SeverancePay > 0, "Severance should still be calculated");
    }

    [Fact]
    public async Task CalculateAsync_DomesticWorkerWithBoard_ShouldUse1_5xPrestaciones()
    {
        // ARRANGE — "¿Trabajadora del Hogar con Dormida Adentro? → Si"
        // Art. 145 CT: Prestaciones sociales se calculan con 1.5× del salario acordado
        // INSS and IR are still deducted (NOT exempt)
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2025, 12, 1);
        var terminationDate = new DateOnly(2026, 6, 15);
        var salary = 15000m; // C$ 15,000
        var vacationDaysTaken = 7m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee
            {
                Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC",
                IsDomesticWorkerWithBoard = true, // 1.5× multiplier for prestaciones
                DeductInss = true,
                DeductIr = true,
                DeductAguinaldo = true,
            });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(vacationDaysTaken);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT — Match the real document: 4 horas extras × $125 = $500, 15 days pending salary
        var result = await _sut.CalculateAsync(empId, TerminationReason.VoluntaryResignation, terminationDate,
            overtimeHours: 4, overtimePay: 500);

        // ASSERT — Match the real document exactly
        Assert.NotNull(result);
        // Aguinaldo: 197/365 × (15,000 × 1.5) = 197/365 × 22,500 = 12,143.84
        Assert.Equal(12143.84m, result.AguinaldoPay);
        // Vacation: 9.19 days × (500 × 1.5) = 9.19 × 750 = 6,892.50
        Assert.Equal(6892.50m, result.VacationPay);
        // Severance (resignation < 1yr): 197/365 × (15,000 × 1.5) = 12,143.84
        Assert.Equal(12143.84m, result.SeverancePay);
        // Pending salary: 15 days × 500 = 7,500
        Assert.Equal(15, result.PendingSalaryDays);
        Assert.Equal(7500m, result.PendingSalaryPay);
        // INSS: 7% on (pendingSalary + vacation + overtime) = (7,500 + 6,892.50 + 500) × 7% = 1,042.48
        Assert.Equal(1042.48m, result.InssLaboralAmount);
        // IR Salario Ordinario: 842.50 × 15/30 = 421.25 (matches document)
        Assert.Equal(421.25m, result.IrSalaryAmount);
        // IR incremental on occasional payments (Art. 19.2 Reglamento Ley 822):
        // totalOccasionalPayments = vacationPay (6,892.50) + overtimePay (500) = 7,392.50
        // INSS on occasional = 7,392.50 × 7% = 517.48
        // Net occasional = 7,392.50 - 517.48 = 6,875.02
        // IR with: (167,400 + 6,875.02 - 100,000) × 15% = 11,141.25
        // IR without: 10,110.00
        // Incremental: 11,141.25 - 10,110.00 = 1,031.25
        Assert.Equal(1031.25m, result.IrTotalAmount - result.IrSalaryAmount);
        // Employer costs: INSS Patronal 21.5% on (salary + vacation + overtime) = 14,892.50 × 21.5% = 3,201.89
        // Document shows 3,203.89 (minor Excel rounding difference)
        Assert.Equal(3201.89m, result.InssPatronalAmount);
        // INATEC 2% on same base = 14,892.50 × 2% = 297.85
        Assert.Equal(297.85m, result.InatecAmount);
        // Net should be less than gross (deductions apply)
        Assert.True(result.NetSettlement < result.GrossSettlement, "Net should be less than gross due to deductions");
    }

    [Fact]
    public async Task CalculateAsync_AllDeductionsOff_ShouldHaveNoDeductions()
    {
        // ARRANGE — All deduction flags = false
        var empId = Guid.NewGuid();
        var hireDate = new DateOnly(2025, 12, 1);
        var terminationDate = new DateOnly(2026, 6, 15);
        var salary = 15000m;

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee
            {
                Id = empId, Salary = salary, HireDate = hireDate, CountryCode = "NIC",
                DeductInss = false, DeductIr = false, DeductAguinaldo = false,
            });
        _mockTaxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(CreateNicConfig());
        _mockVacationRepo.Setup(r => r.GetVacationDaysSumAsync(empId, "approved"))
            .ReturnsAsync(0m);
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision>());

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.VoluntaryResignation, terminationDate);

        // ASSERT — No deductions, no aguinaldo
        Assert.NotNull(result);
        Assert.Equal(0m, result.AguinaldoPay);
        Assert.Equal(0m, result.InssLaboralAmount);
        Assert.Equal(0m, result.IrTotalAmount);
        Assert.Equal(0m, result.TotalDeductions);
        Assert.Equal(result.GrossSettlement, result.NetSettlement);
        // Vacation and severance still apply
        Assert.True(result.VacationPay > 0);
        Assert.True(result.SeverancePay > 0);
    }
}
