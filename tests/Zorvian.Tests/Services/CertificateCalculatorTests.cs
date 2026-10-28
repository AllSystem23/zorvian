using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

/// <summary>
/// La liquidación final dejó de estar fijada a Nicaragua: las tasas, topes, tabla de
/// IR y días de prestaciones salen de <see cref="CountryTaxConfig"/>. Estos tests
/// verifican el cálculo por país, el tope de la seguridad social, la fórmula
/// escalonada vs plana de indemnización, el período de aguinaldo y el respaldo a
/// Nicaragua cuando el país no está configurado.
/// </summary>
public sealed class CertificateCalculatorTests : IDisposable
{
    private readonly ZorvianDbContext _db;
    private readonly Mock<ICountryTaxConfigRepository> _taxRepo = new();
    private readonly TenantId _tenantId = new(Guid.NewGuid());

    public CertificateCalculatorTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantMock = new Mock<Zorvian.Core.Interfaces.ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, tenantMock.Object);
    }

    public void Dispose() => _db.Dispose();

    // ── Configuraciones por país (espejo de CountryTaxConfigSeeder) ──

    private static CountryTaxConfig Nicaragua() => new()
    {
        CountryCode = "NIC",
        CountryName = "Nicaragua",
        Currency = "NIO",
        InssEmployeeRate = 0.07m,
        InssEmployeeMax = 84764m,
        InssEmployerRate = 0.225m,
        OtherEmployerRate = 0.02m,
        OtherEmployerName = "INATEC",
        IrTableJson = "[{\"min\":0,\"max\":100000,\"rate\":0},{\"min\":100001,\"max\":200000,\"rate\":0.15}]",
        VacationDaysPerYear = 30,
        IndemnityDaysPerYear = 30,
        MaxIndemnityYears = 12,
        IndemnityTiersJson = "[{\"upToYears\":3,\"daysPerYear\":30},{\"upToYears\":6,\"daysPerYear\":20}]",
        MaxIndemnityDays = 150,
        AguinaldoPeriodStartMonth = 12,
        AguinaldoPeriodStartDay = 1,
    };

    private static CountryTaxConfig CostaRica() => new()
    {
        CountryCode = "CRI",
        CountryName = "Costa Rica",
        Currency = "CRC",
        InssEmployeeRate = 0.1083m,
        InssEmployeeMax = 863083m,
        InssEmployerRate = 0.2683m,
        OtherEmployerRate = 0m,
        OtherEmployerName = null,
        IrTableJson = "[{\"min\":0,\"max\":932000,\"rate\":0}]",
        VacationDaysPerYear = 12,
        IndemnityDaysPerYear = 24,
        MaxIndemnityYears = 12,
        IndemnityTiersJson = "[]",
        MaxIndemnityDays = 0,
        AguinaldoPeriodStartMonth = 12,
        AguinaldoPeriodStartDay = 1,
    };

    private CertificateCalculator CreateSut(CountryTaxConfig? config)
    {
        _taxRepo
            .Setup(r => r.GetByCountryCodeAsync(It.IsAny<string>()))
            .ReturnsAsync(config);
        return new CertificateCalculator(_db, _taxRepo.Object);
    }

    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly DateOnly HireDate = new(2020, 1, 15);
    private static readonly DateOnly TermDate = new(2026, 10, 1);

    [Fact]
    public async Task Nicaragua_UsesConfigRatesTieredSeveranceAndAguinaldoPeriod()
    {
        var sut = CreateSut(Nicaragua());

        var vars = await sut.CalculateLiquidationVariablesAsync(
            EmployeeId, 15000m, HireDate, TermDate, "Renuncia voluntaria", "NIC");

        Assert.Equal("500.00", vars["sueldo_diario"]);
        // Deducciones del trabajador desde CountryTaxConfig
        Assert.Equal("1,050.00", vars["inss_laboral"]);          // 15,000 × 7%
        Assert.Equal("842.50", vars["ir_ord"]);                   // tabla NIC proyectada a 12 meses
        Assert.Equal("7.00%", vars["inss_tasa"]);
        // Aportes patronales de referencia
        Assert.Equal("3,375.00", vars["inss_patronal"]);          // 15,000 × 22.5%
        Assert.Equal("22.50%", vars["inss_patronal_tasa"]);
        Assert.Equal("INATEC", vars["other_employer_name"]);
        Assert.Equal("300.00", vars["other_employer_monto"]);     // 15,000 × 2%
        Assert.Equal("2.00%", vars["other_employer_tasa"]);
        // Prestaciones
        Assert.Equal("30", vars["vacaciones_dias"]);              // VacationDaysPerYear (sin saldo)
        Assert.Equal("150", vars["indemnizacion_dias"]);          // escalones Art. 45 + tope
        Assert.Equal("75,000.00", vars["indemnizacion_monto"]);   // 150 días × 500
        Assert.Equal("01/12/2025 - 01/10/2026", vars["periodo_aguinaldo"]);
        Assert.Equal("12,534.25", vars["aguinaldo_monto"]);       // 305/365 × salario
        // Monto en letras con la moneda del país
        Assert.Contains("córdobas", vars["neto_en_letras"]);
    }

    [Fact]
    public async Task CostaRica_UsesCountryRates_AndHidesOtherEmployerContribution()
    {
        var sut = CreateSut(CostaRica());

        var vars = await sut.CalculateLiquidationVariablesAsync(
            EmployeeId, 1_000_000m, HireDate, TermDate, "Despido", "CRI");

        Assert.Equal("108,300.00", vars["inss_laboral"]);         // 10.83%
        Assert.Equal("268,300.00", vars["inss_patronal"]);        // 26.83%
        Assert.Equal("10.83%", vars["inss_tasa"]);
        Assert.Equal("26.83%", vars["inss_patronal_tasa"]);
        Assert.Equal("", vars["other_employer_name"]);            // CR no tiene otro aporte
        Assert.Equal("0.00%", vars["other_employer_tasa"]);
        Assert.Equal("12", vars["vacaciones_dias"]);
        // Sin escalones configurados → fórmula plana 24 días/año (tope 12 años).
        Assert.Equal("144", vars["indemnizacion_dias"]);
        Assert.Equal("4,799,999.52", vars["indemnizacion_monto"]);
        Assert.Contains("colones costarricenses", vars["neto_en_letras"]);
    }

    [Fact]
    public async Task UnknownCountry_FallsBackToNicaraguaDefaults()
    {
        var sut = CreateSut(config: null);

        var vars = await sut.CalculateLiquidationVariablesAsync(
            EmployeeId, 15000m, HireDate, TermDate, null, countryCode: null);

        Assert.Equal("7.00%", vars["inss_tasa"]);
        Assert.Equal("22.50%", vars["inss_patronal_tasa"]);
        Assert.Equal("INATEC", vars["other_employer_name"]);
        Assert.Contains("córdobas", vars["neto_en_letras"]);
        Assert.Equal("Voluntario", vars["retiro_reason"]);
    }

    [Fact]
    public async Task InssEmployeeMax_CapsTheWorkerContribution()
    {
        var sut = CreateSut(Nicaragua());

        var vars = await sut.CalculateLiquidationVariablesAsync(
            EmployeeId, 1_500_000m, HireDate, TermDate, null, "NIC");

        // 7% de 1,500,000 = 105,000 → tope del catálogo (84,764).
        Assert.Equal("84,764.00", vars["inss_laboral"]);
    }

    [Fact]
    public async Task LeaveBalance_OverridesAnnualVacationEntitlement()
    {
        _db.LeaveBalances.Add(new LeaveBalances
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId.ToString(),
            EmployeeId = EmployeeId,
            Year = DateTime.Now.Year,
            VacationDaysAccrued = 10m,
            VacationDaysTaken = 5m,
            VacationDaysPending = 0m,
        });
        await _db.SaveChangesAsync();

        var sut = CreateSut(Nicaragua());

        var vars = await sut.CalculateLiquidationVariablesAsync(
            EmployeeId, 15000m, HireDate, TermDate, null, "NIC");

        Assert.Equal("5", vars["vacaciones_dias"]);
        Assert.Equal("2,500.00", vars["vacaciones_monto"]);
    }

    [Fact]
    public async Task AguinaldoPeriod_StartsAtHireDate_WhenHiredInTheTerminationYear()
    {
        var sut = CreateSut(Nicaragua());

        var vars = await sut.CalculateLiquidationVariablesAsync(
            EmployeeId, 15000m, new DateOnly(2026, 3, 1), TermDate, null, "NIC");

        Assert.Equal("01/03/2026 - 01/10/2026", vars["periodo_aguinaldo"]);
        Assert.Equal("8,835.62", vars["aguinaldo_monto"]);   // 215/365 × 15,000
    }

    [Theory]
    // mes/día inválidos → default 12/1; mes válido con día inexistente → se corrige el día
    [InlineData(0, 1, "01/12/2025 - 01/10/2026")]     // lo que dejaba en cero el formulario viejo
    [InlineData(12, 0, "01/12/2025 - 01/10/2026")]
    [InlineData(13, 1, "01/12/2025 - 01/10/2026")]    // mes inexistente
    [InlineData(4, 31, "01/04/2025 - 01/10/2026")]    // 31 no existe en abril
    public async Task AguinaldoPeriodWithInvalidMonthOrDay_FallsBackInsteadOfThrowing(
        int month, int day, string expectedPeriod)
    {
        // Regresión del error "Year, Month, and Day parameters describe an
        // un-representable DateTime." que tumbaba GET documents/entity-context
        // (paso 1 del wizard) y el cálculo del finiquito.
        var config = Nicaragua();
        config.AguinaldoPeriodStartMonth = month;
        config.AguinaldoPeriodStartDay = day;
        var sut = CreateSut(config);

        var vars = await sut.CalculateLiquidationVariablesAsync(
            EmployeeId, 15000m, HireDate, TermDate, null, "NIC");

        Assert.Equal(expectedPeriod, vars["periodo_aguinaldo"]);
    }
}
