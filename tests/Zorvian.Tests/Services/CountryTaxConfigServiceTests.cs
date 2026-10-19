using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Tax;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Catálogo fiscal global (SuperAdmin) + ajuste por compañía. La pantalla
/// /admin/country-tax-configs reescribe la fila COMPLETA del país, así que el servicio
/// debe: (1) persistir todos los campos (nada de perder regímenes INSS, escalones de
/// indemnización o el período de aguinaldo), (2) rechazar tasas fuera de 0..1 (un `7`
/// en lugar de `0.07` produciría deducciones de 700 %), (3) no dejar la entidad a medio
/// escribir cuando el payload es inválido y (4) que el ajuste de una compañía NUNCA
/// toque la fila global ni la de otra compañía.
/// </summary>
public sealed class CountryTaxConfigServiceTests
{
    private const string CompanyTenant = "11111111-1111-1111-1111-111111111111";
    private const string ValidIrTable =
        "[{\"min\":0,\"max\":100000,\"rate\":0},{\"min\":100000,\"max\":200000,\"rate\":0.15}]";
    private const string ValidTiers =
        "[{\"upToYears\":3,\"daysPerYear\":30},{\"upToYears\":6,\"daysPerYear\":20}]";

    private readonly Mock<ICountryTaxConfigRepository> _repo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly CountryTaxConfigService _sut;

    public CountryTaxConfigServiceTests()
    {
        _tenant.SetupGet(t => t.TenantId).Returns(TenantId.FromString(CompanyTenant));
        _companyRepo.Setup(r => r.GetByTenantIdAsync(CompanyTenant))
            .ReturnsAsync(new Company { Country = "Nicaragua" });

        _sut = new CountryTaxConfigService(_repo.Object, _companyRepo.Object, _tenant.Object);
        _repo.Setup(r => r.ExistsByCountryCodeAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(false);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
    }

    private static CreateCountryTaxConfigRequest CreateRequest(
        string countryCode = "NIC",
        decimal inssEmployeeRate = 0.07m,
        decimal inssEmployerRate = 0.215m,
        decimal otherEmployerRate = 0.02m,
        string? otherEmployerName = "INATEC",
        decimal christmasBonusPercentage = 0.0833m,
        string irTableJson = ValidIrTable,
        string indemnityTiersJson = ValidTiers,
        decimal inssIntegralEmployeeRate = 0.045m,
        decimal inssIntegralEmployerRate = 0.225m,
        decimal inssIntegralEmployerRateSmall = 0.215m,
        decimal inssIvmEmployeeRate = 0.07m,
        decimal inssIvmEmployerRate = 0.19m,
        decimal inssIvmEmployerRateSmall = 0.18m,
        int inssSmallEmployerThreshold = 50,
        int maxIndemnityDays = 150,
        int aguinaldoPeriodStartMonth = 12,
        int aguinaldoPeriodStartDay = 1,
        int defaultFiscalStartMonth = 1) =>
        new(
            CountryCode: countryCode,
            CountryName: "Nicaragua",
            Currency: "NIO",
            InssEmployeeRate: inssEmployeeRate,
            InssEmployeeMax: 20000m,
            InssEmployerRate: inssEmployerRate,
            InssEmployerMax: 0m,
            OtherEmployerRate: otherEmployerRate,
            OtherEmployerName: otherEmployerName,
            IrExemptAmount: 100000m,
            IrTableJson: irTableJson,
            VacationDaysPerYear: 15,
            ChristmasBonusPercentage: christmasBonusPercentage,
            IndemnityDaysPerYear: 30,
            MaxIndemnityYears: 5,
            HasThirteenthMonth: true,
            HasFourteenthMonth: false,
            InssIntegralEmployeeRate: inssIntegralEmployeeRate,
            InssIntegralEmployerRate: inssIntegralEmployerRate,
            InssIntegralEmployerRateSmall: inssIntegralEmployerRateSmall,
            InssIvmEmployeeRate: inssIvmEmployeeRate,
            InssIvmEmployerRate: inssIvmEmployerRate,
            InssIvmEmployerRateSmall: inssIvmEmployerRateSmall,
            InssSmallEmployerThreshold: inssSmallEmployerThreshold,
            IndemnityTiersJson: indemnityTiersJson,
            MaxIndemnityDays: maxIndemnityDays,
            AguinaldoPeriodStartMonth: aguinaldoPeriodStartMonth,
            AguinaldoPeriodStartDay: aguinaldoPeriodStartDay,
            DefaultFiscalStartMonth: defaultFiscalStartMonth);

    private static UpdateCountryTaxConfigRequest UpdateRequest(
        decimal? inssEmployeeRate = null,
        decimal? inssEmployerRate = null,
        decimal? otherEmployerRate = null,
        decimal? christmasBonusPercentage = null,
        string? otherEmployerName = null,
        string? irTableJson = null,
        string? indemnityTiersJson = null,
        decimal? inssIntegralEmployeeRate = null,
        decimal? inssIntegralEmployerRate = null,
        decimal? inssIntegralEmployerRateSmall = null,
        decimal? inssIvmEmployeeRate = null,
        decimal? inssIvmEmployerRate = null,
        decimal? inssIvmEmployerRateSmall = null,
        int? inssSmallEmployerThreshold = null,
        int? maxIndemnityDays = null,
        int? aguinaldoPeriodStartMonth = null,
        int? aguinaldoPeriodStartDay = null,
        int? defaultFiscalStartMonth = null,
        bool? isActive = null) =>
        new(
            CountryCode: null,
            CountryName: null,
            Currency: null,
            InssEmployeeRate: inssEmployeeRate,
            InssEmployeeMax: null,
            InssEmployerRate: inssEmployerRate,
            InssEmployerMax: null,
            OtherEmployerRate: otherEmployerRate,
            OtherEmployerName: otherEmployerName,
            IrExemptAmount: null,
            IrTableJson: irTableJson,
            VacationDaysPerYear: null,
            ChristmasBonusPercentage: christmasBonusPercentage,
            IndemnityDaysPerYear: null,
            MaxIndemnityYears: null,
            HasThirteenthMonth: null,
            HasFourteenthMonth: null,
            IsActive: isActive,
            InssIntegralEmployeeRate: inssIntegralEmployeeRate,
            InssIntegralEmployerRate: inssIntegralEmployerRate,
            InssIntegralEmployerRateSmall: inssIntegralEmployerRateSmall,
            InssIvmEmployeeRate: inssIvmEmployeeRate,
            InssIvmEmployerRate: inssIvmEmployerRate,
            InssIvmEmployerRateSmall: inssIvmEmployerRateSmall,
            InssSmallEmployerThreshold: inssSmallEmployerThreshold,
            IndemnityTiersJson: indemnityTiersJson,
            MaxIndemnityDays: maxIndemnityDays,
            AguinaldoPeriodStartMonth: aguinaldoPeriodStartMonth,
            AguinaldoPeriodStartDay: aguinaldoPeriodStartDay,
            DefaultFiscalStartMonth: defaultFiscalStartMonth);

    private CountryTaxConfig SeedEntity()
    {
        var entity = new CountryTaxConfig
        {
            CountryCode = "NIC",
            CountryName = "Nicaragua",
            Currency = "NIO",
            InssEmployeeRate = 0.07m,
            InssEmployerRate = 0.215m,
            OtherEmployerName = "INATEC",
        };
        _repo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<CountryTaxConfig>())).Returns(Task.CompletedTask);
        return entity;
    }

    // ── Create ──

    [Fact]
    public async Task CreateAsync_PersistsAllFields_IncludingRegimesTiersAndAguinaldo()
    {
        CountryTaxConfig? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<CountryTaxConfig>()))
            .Callback<CountryTaxConfig>(e => saved = e)
            .Returns(Task.CompletedTask);

        var response = await _sut.CreateAsync(CreateRequest(countryCode: "nic"));

        saved.Should().NotBeNull();
        saved!.CountryCode.Should().Be("NIC");
        saved.OtherEmployerName.Should().Be("INATEC");
        saved.InssIntegralEmployeeRate.Should().Be(0.045m);
        saved.InssIntegralEmployerRate.Should().Be(0.225m);
        saved.InssIntegralEmployerRateSmall.Should().Be(0.215m);
        saved.InssIvmEmployeeRate.Should().Be(0.07m);
        saved.InssIvmEmployerRate.Should().Be(0.19m);
        saved.InssIvmEmployerRateSmall.Should().Be(0.18m);
        saved.InssSmallEmployerThreshold.Should().Be(50);
        saved.IndemnityTiersJson.Should().Be(ValidTiers);
        saved.MaxIndemnityDays.Should().Be(150);
        saved.AguinaldoPeriodStartMonth.Should().Be(12);
        saved.AguinaldoPeriodStartDay.Should().Be(1);
        saved.DefaultFiscalStartMonth.Should().Be(1);
        saved.IrTableJson.Should().Be(ValidIrTable);

        response.InssIntegralEmployerRate.Should().Be(0.225m);
        response.IndemnityTiersJson.Should().Be(ValidTiers);
        response.MaxIndemnityDays.Should().Be(150);
    }

    [Theory]
    [InlineData(7)]     // 7 en lugar de 0.07
    [InlineData(1.01)]
    [InlineData(-0.01)]
    public async Task CreateAsync_RejectsRateOutsideFractionRange(decimal rate)
    {
        var act = () => _sut.CreateAsync(CreateRequest(inssEmployeeRate: rate));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*entre 0 y 1*");

        _repo.Verify(r => r.AddAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
    }

    [Theory]
    [InlineData("no-es-json")]
    [InlineData("{\"rate\":0.15}")]                          // no es un arreglo
    [InlineData("[{\"min\":0,\"max\":1000,\"rate\":15}]")]    // 1500 %
    [InlineData("[{\"min\":0,\"max\":1000}]")]                // sin rate
    public async Task CreateAsync_RejectsInvalidIrTable(string irTableJson)
    {
        var act = () => _sut.CreateAsync(CreateRequest(irTableJson: irTableJson));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tabla de IR*");

        _repo.Verify(r => r.AddAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
    }

    [Theory]
    [InlineData("[{\"upToYears\":0,\"daysPerYear\":30}]")]
    [InlineData("[{\"upToYears\":3,\"daysPerYear\":0}]")]
    [InlineData("[{\"upToYears\":3}]")]
    [InlineData("[{\"daysPerYear\":30}]")]
    public async Task CreateAsync_RejectsInvalidIndemnityTiers(string tiersJson)
    {
        var act = () => _sut.CreateAsync(CreateRequest(indemnityTiersJson: tiersJson));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*escalón*");

        _repo.Verify(r => r.AddAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_NormalizesBlankOtherEmployerName_ToNull(string? name)
    {
        CountryTaxConfig? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<CountryTaxConfig>()))
            .Callback<CountryTaxConfig>(e => saved = e)
            .Returns(Task.CompletedTask);

        var response = await _sut.CreateAsync(CreateRequest(otherEmployerName: name, otherEmployerRate: 0m));

        saved!.OtherEmployerName.Should().BeNull();
        response.OtherEmployerName.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_AcceptsZeroRates_ForCountriesWithoutRegimes()
    {
        // 0 es válido: significa "no aplica", no "sin configurar".
        CountryTaxConfig? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<CountryTaxConfig>()))
            .Callback<CountryTaxConfig>(e => saved = e)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(CreateRequest(
            inssIntegralEmployeeRate: 0m,
            inssIntegralEmployerRate: 0m,
            inssIntegralEmployerRateSmall: 0m,
            inssIvmEmployeeRate: 0m,
            inssIvmEmployerRate: 0m,
            inssIvmEmployerRateSmall: 0m,
            otherEmployerRate: 0m,
            otherEmployerName: null,
            indemnityTiersJson: "[]"));

        saved!.InssIntegralEmployeeRate.Should().Be(0m);
        saved.IndemnityTiersJson.Should().Be("[]");
    }

    // ── Update ──

    [Fact]
    public async Task UpdateAsync_WritesRegimesTiersAndAguinaldo()
    {
        var entity = SeedEntity();

        var response = await _sut.UpdateAsync(entity.Id, UpdateRequest(
            inssIntegralEmployeeRate: 0.045m,
            inssIntegralEmployerRate: 0.225m,
            inssIntegralEmployerRateSmall: 0.215m,
            inssIvmEmployeeRate: 0.07m,
            inssIvmEmployerRate: 0.19m,
            inssIvmEmployerRateSmall: 0.18m,
            inssSmallEmployerThreshold: 30,
            indemnityTiersJson: ValidTiers,
            maxIndemnityDays: 150,
            aguinaldoPeriodStartMonth: 1,
            aguinaldoPeriodStartDay: 15,
            defaultFiscalStartMonth: 7,
            isActive: false));

        entity.InssIntegralEmployeeRate.Should().Be(0.045m);
        entity.InssIntegralEmployerRate.Should().Be(0.225m);
        entity.InssSmallEmployerThreshold.Should().Be(30);
        entity.IndemnityTiersJson.Should().Be(ValidTiers);
        entity.MaxIndemnityDays.Should().Be(150);
        entity.AguinaldoPeriodStartMonth.Should().Be(1);
        entity.AguinaldoPeriodStartDay.Should().Be(15);
        entity.DefaultFiscalStartMonth.Should().Be(7);
        entity.IsActive.Should().BeFalse();

        response!.DefaultFiscalStartMonth.Should().Be(7);
        _repo.Verify(r => r.UpdateAsync(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RejectsOutOfRangeRate_WithoutMutatingNorPersisting()
    {
        var entity = SeedEntity();

        var act = () => _sut.UpdateAsync(entity.Id, UpdateRequest(inssEmployerRate: 21.5m));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INSS patronal*");

        entity.InssEmployerRate.Should().Be(0.215m); // intacto: se valida antes de mutar
        _repo.Verify(r => r.UpdateAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RejectsInvalidIrTable_WithoutPersisting()
    {
        var entity = SeedEntity();

        var act = () => _sut.UpdateAsync(entity.Id, UpdateRequest(irTableJson: "[{\"rate\":15}]"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tabla de IR*");

        _repo.Verify(r => r.UpdateAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyIsActive_LeavesRatesAndTablesUntouched()
    {
        var entity = SeedEntity();
        entity.IrTableJson = ValidIrTable;
        entity.IndemnityTiersJson = ValidTiers;

        await _sut.UpdateAsync(entity.Id, UpdateRequest(isActive: false));

        entity.InssEmployeeRate.Should().Be(0.07m);
        entity.InssEmployerRate.Should().Be(0.215m);
        entity.OtherEmployerName.Should().Be("INATEC");
        entity.IrTableJson.Should().Be(ValidIrTable);
        entity.IndemnityTiersJson.Should().Be(ValidTiers);
        entity.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_BlankOtherEmployerName_ClearsIt()
    {
        var entity = SeedEntity();

        await _sut.UpdateAsync(entity.Id, UpdateRequest(otherEmployerName: "  "));

        entity.OtherEmployerName.Should().BeNull();
    }

    // ── Lectura ──

    [Fact]
    public async Task GetAllAsync_MapsNewFieldsToResponse()
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new CountryTaxConfig
            {
                CountryCode = "NIC",
                CountryName = "Nicaragua",
                Currency = "NIO",
                InssIntegralEmployerRate = 0.225m,
                IndemnityTiersJson = ValidTiers,
                MaxIndemnityDays = 150,
                AguinaldoPeriodStartMonth = 12,
                AguinaldoPeriodStartDay = 1,
                DefaultFiscalStartMonth = 1,
                IrTableJson = ValidIrTable,
            }
        ]);

        var items = await _sut.GetAllAsync();

        items.Should().HaveCount(1);
        items[0].InssIntegralEmployerRate.Should().Be(0.225m);
        items[0].IndemnityTiersJson.Should().Be(ValidTiers);
        items[0].MaxIndemnityDays.Should().Be(150);
        items[0].AguinaldoPeriodStartMonth.Should().Be(12);
        items[0].IrTableJson.Should().Be(ValidIrTable);
    }

    // ── Ajuste propio de la compañía ──

    private static CountryTaxConfig GlobalNicaragua() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = CountryTaxConfig.GlobalTenantId,
        CountryCode = "NIC",
        CountryName = "Nicaragua",
        Currency = "NIO",
        InssEmployeeRate = 0.07m,
        InssEmployeeMax = 20000m,
        InssEmployerRate = 0.225m,
        OtherEmployerRate = 0.02m,
        OtherEmployerName = "INATEC",
        IrExemptAmount = 100000m,
        IrTableJson = ValidIrTable,
        VacationDaysPerYear = 15,
        IndemnityTiersJson = ValidTiers,
        MaxIndemnityDays = 150,
        AguinaldoPeriodStartMonth = 12,
        AguinaldoPeriodStartDay = 1,
        DefaultFiscalStartMonth = 1,
        IsActive = true,
    };

    [Fact]
    public async Task GetMyCountryAsync_WithoutOverride_ReturnsGlobalAndMarksNotCustomized()
    {
        _repo.Setup(r => r.GetTenantOverrideAsync("NIC", CompanyTenant))
            .ReturnsAsync((CountryTaxConfig?)null);
        _repo.Setup(r => r.GetGlobalByCountryCodeAsync("NIC")).ReturnsAsync(GlobalNicaragua());

        var result = await _sut.GetMyCountryAsync();

        result.Should().NotBeNull();
        result!.CountryCode.Should().Be("NIC");
        result.CountryName.Should().Be("Nicaragua");
        result.IsCustomized.Should().BeFalse();
        result.Config!.InssEmployerRate.Should().Be(0.225m);
    }

    [Fact]
    public async Task GetMyCountryAsync_WithOverride_UsesOwnRowAndMarksCustomized()
    {
        var own = GlobalNicaragua();
        own.TenantId = CompanyTenant;
        own.InssEmployerRate = 0.23m;
        _repo.Setup(r => r.GetTenantOverrideAsync("NIC", CompanyTenant)).ReturnsAsync(own);

        var result = await _sut.GetMyCountryAsync();

        result!.IsCustomized.Should().BeTrue();
        result.Config!.InssEmployerRate.Should().Be(0.23m);
        _repo.Verify(r => r.GetGlobalByCountryCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMyCountryAsync_GlobalContext_ReturnsNull()
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.TenantId).Returns(TenantId.FromString(CountryTaxConfig.GlobalTenantId));
        var sut = new CountryTaxConfigService(_repo.Object, _companyRepo.Object, tenant.Object);

        var result = await sut.GetMyCountryAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMyCountryAsync_CompanyWithoutCountry_ReturnsEmptyPayload()
    {
        _companyRepo.Setup(r => r.GetByTenantIdAsync(CompanyTenant))
            .ReturnsAsync(new Company { Country = "" });

        var result = await _sut.GetMyCountryAsync();

        result.Should().NotBeNull();
        result!.CountryCode.Should().BeEmpty();
        result.Config.Should().BeNull();
    }

    [Fact]
    public async Task UpsertMyCountryAsync_CreatesOverrideFromGlobal_AndInheritsOmittedFields()
    {
        var global = GlobalNicaragua();
        _repo.Setup(r => r.GetTenantOverrideAsync("NIC", CompanyTenant))
            .ReturnsAsync((CountryTaxConfig?)null);
        _repo.Setup(r => r.GetGlobalByCountryCodeAsync("NIC")).ReturnsAsync(global);

        CountryTaxConfig? added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<CountryTaxConfig>()))
            .Callback<CountryTaxConfig>(e => added = e)
            .Returns(Task.CompletedTask);

        var result = await _sut.UpsertMyCountryAsync(UpdateRequest(inssEmployerRate: 0.23m));

        added.Should().NotBeNull();
        added!.TenantId.Should().Be(CompanyTenant);
        added.CompanyId.Should().Be(Guid.Parse(CompanyTenant));
        added.InssEmployerRate.Should().Be(0.23m);
        // Lo no enviado se hereda del catálogo: crearla en ceros borraría IR, escalones…
        added.IrTableJson.Should().Be(ValidIrTable);
        added.IndemnityTiersJson.Should().Be(ValidTiers);
        added.MaxIndemnityDays.Should().Be(150);
        added.OtherEmployerName.Should().Be("INATEC");
        result.IsCustomized.Should().BeTrue();
        _repo.Verify(r => r.UpdateAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
        // La fila global permanece intacta.
        global.InssEmployerRate.Should().Be(0.225m);
    }

    [Fact]
    public async Task UpsertMyCountryAsync_ExistingOverride_UpdatesItInPlace()
    {
        var own = GlobalNicaragua();
        own.TenantId = CompanyTenant;
        _repo.Setup(r => r.GetTenantOverrideAsync("NIC", CompanyTenant)).ReturnsAsync(own);

        await _sut.UpsertMyCountryAsync(UpdateRequest(inssEmployeeRate: 0.06m));

        own.InssEmployeeRate.Should().Be(0.06m);
        _repo.Verify(r => r.UpdateAsync(own), Times.Once);
        _repo.Verify(r => r.AddAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
    }

    [Fact]
    public async Task UpsertMyCountryAsync_WithoutGlobalCatalog_Throws()
    {
        _repo.Setup(r => r.GetTenantOverrideAsync("NIC", CompanyTenant))
            .ReturnsAsync((CountryTaxConfig?)null);
        _repo.Setup(r => r.GetGlobalByCountryCodeAsync("NIC"))
            .ReturnsAsync((CountryTaxConfig?)null);

        var act = () => _sut.UpsertMyCountryAsync(UpdateRequest(inssEmployerRate: 0.06m));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*global*");
        _repo.Verify(r => r.AddAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
    }

    [Fact]
    public async Task UpsertMyCountryAsync_InvalidRate_ThrowsAndDoesNotPersist()
    {
        var act = () => _sut.UpsertMyCountryAsync(UpdateRequest(inssEmployeeRate: 7m));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(r => r.AddAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ResetMyCountryAsync_DeletesOwnRow()
    {
        var own = GlobalNicaragua();
        own.TenantId = CompanyTenant;
        _repo.Setup(r => r.GetTenantOverrideAsync("NIC", CompanyTenant)).ReturnsAsync(own);

        var deleted = await _sut.ResetMyCountryAsync();

        deleted.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(own), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ResetMyCountryAsync_WithoutOverride_ReturnsFalse()
    {
        _repo.Setup(r => r.GetTenantOverrideAsync("NIC", CompanyTenant))
            .ReturnsAsync((CountryTaxConfig?)null);

        var deleted = await _sut.ResetMyCountryAsync();

        deleted.Should().BeFalse();
        _repo.Verify(r => r.DeleteAsync(It.IsAny<CountryTaxConfig>()), Times.Never);
    }
}
