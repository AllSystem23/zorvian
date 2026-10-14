using FluentAssertions;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.Documentary;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// El contrato multi-país se nutre del contexto de entidad: país desde
/// Company.Country (via FiscalYearHelper), ciudad desde la sucursal del empleado
/// y datos fiscales/laborales por país. Estos tests fijan ese contrato de datos.
/// </summary>
public sealed class DocumentGenerationServiceContractContextTests
{
    private readonly Mock<IDocumentService> _documentService = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<IClientRepository> _clientRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();

    private DocumentGenerationService CreateSut()
    {
        _tenant.Setup(t => t.TenantId).Returns("tenant-1");
        return new DocumentGenerationService(
            _documentService.Object,
            _employeeRepo.Object,
            _saleRepo.Object,
            _companyRepo.Object,
            _clientRepo.Object,
            _tenant.Object);
    }

    private void SetupCompany(Company? company) =>
        _companyRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<string>())).ReturnsAsync(company);

    private static Company MakeCompany(string? country) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = "tenant-1",
        Name = "Comercial Zorvian",
        Country = country ?? string.Empty,
        TaxId = "J0310000000000",
        Address = "Central, Managua",
    };

    private static Employee MakeEmployee(Branch? branch) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = "tenant-1",
        FirstName = "María",
        LastName = "López",
        IdentificationNumber = "0101 12345 678",
        Position = "Coordinadora",
        HireDate = new DateOnly(2026, 11, 1),
        Salary = 8500m,
        Address = "Residencial Las Palmas",
        BranchId = branch?.Id,
        Branch = branch,
    };

    private void SetupEmployee(Employee employee) =>
        _employeeRepo.Setup(r => r.GetByIdWithBranchAsync(employee.Id)).ReturnsAsync(employee);

    [Fact]
    public async Task Employee_WithBranch_ExposesCountryAndBranchContractData()
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-1",
            Name = "Sucursal Escalante",
            Address = "Barrio Escalante, San José",
        };
        var employee = MakeEmployee(branch);
        SetupEmployee(employee);
        SetupCompany(MakeCompany("Costa Rica"));

        var ctx = await CreateSut().GetEntityContextAsync("employee", employee.Id);

        ctx.Should().NotBeNull();
        var data = ctx!.Data;

        // País: ISO-3 vía FiscalYearHelper + datos del contrato
        data["Company.CountryCode"].Should().Be("CRI");
        data["Country.Code"].Should().Be("CRI");
        data["Country.Name"].Should().Be("Costa Rica");
        data["Country.Currency"].Should().Be("₡");
        data["Country.CurrencyName"].Should().Be("colones costarricenses");
        data["Country.WeeklyHours"].Should().Be("48");
        data["Country.WeeklySchedule"].Should().Be("de lunes a viernes");
        data["Country.ProbationMonths"].Should().Be("2");
        data["Country.VacationDays"].Should().Be("14");
        data["Country.IdDocument"].Should().Be("Cédula de Identidad");
        data["Country.LaborLaw"].Should().Contain("Costa Rica");
        data["Country.TaxAuthority"].Should().Be("Ministerio de Hacienda");
        data["Country.SocialSecurity"].Should().Contain("CCSS");
        data["Country.ThirteenthMonth"].Should().Be("Aguinaldo");

        // Etiqueta de identificación fiscal = tipo de ID (RUC/NIT/...), no la entidad
        data["Company.TaxIdLabel"].Should().Be("Cédula Jurídica");

        // Lugar de emisión: la compañía (de allí "sale" el contrato), no la sucursal
        data["Branch.Name"].Should().Be("Sucursal Escalante");
        data["Branch.Address"].Should().Be("Barrio Escalante, San José");
        data["Contract.City"].Should().Be("Central, Managua");

        // Company.LegalName vacío → fallback a Name
        data["Company.LegalName"].Should().Be("Comercial Zorvian");

        // Claves planas del wizard
        data["employee_name"].Should().Be("María López");
        data["employee_id"].Should().Be("0101 12345 678");
        data["position"].Should().Be("Coordinadora");
        data["salary"].Should().Be(8500m.ToString("N2")); // misma cultura que el servicio
        data["start_date"].Should().Be("01/11/2026");
    }

    [Fact]
    public async Task Employee_WithoutBranch_ContractCityFallsBackToCompanyAddress()
    {
        var employee = MakeEmployee(branch: null);
        SetupEmployee(employee);
        SetupCompany(MakeCompany("Guatemala"));

        var data = (await CreateSut().GetEntityContextAsync("employee", employee.Id))!.Data;

        data["Branch.Address"].Should().Be("");
        data["Contract.City"].Should().Be("Central, Managua");
        data["Country.Code"].Should().Be("GTM");
        data["Country.Currency"].Should().Be("Q");
        data["Country.WeeklyHours"].Should().Be("44");
        data["Country.WeeklySchedule"].Should().Be("de lunes a viernes");
        data["Country.TaxAuthority"].Should().Contain("SAT");
        data["Country.SocialSecurity"].Should().Contain("IGSS");
    }

    [Fact]
    public async Task UnknownOrMissingCountry_DefaultsToNicaragua()
    {
        var employee = MakeEmployee(branch: null);
        SetupEmployee(employee);
        SetupCompany(MakeCompany(country: null));

        var data = (await CreateSut().GetEntityContextAsync("employee", employee.Id))!.Data;

        data["Company.CountryCode"].Should().Be("NIC");
        data["Country.Name"].Should().Be("Nicaragua");
        data["Country.Currency"].Should().Be("C$");
        data["Country.WeeklyHours"].Should().Be("48");
        data["Country.WeeklySchedule"].Should().Be("de lunes a sábado");
        data["Company.TaxIdLabel"].Should().Be("RUC");
        data["Country.SocialSecurity"].Should().Contain("INSS");
        data["Country.LaborLaw"].Should().Contain("Nicaragua");
    }

    [Fact]
    public async Task CompanyWithoutAddress_ContractCityFallsBackToBranch()
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-1",
            Name = "Sucursal Norte",
            Address = "Km 5 Carretera a Masaya",
        };
        var employee = MakeEmployee(branch);
        SetupEmployee(employee);
        var company = MakeCompany("Nicaragua");
        company.Address = null;
        SetupCompany(company);

        var data = (await CreateSut().GetEntityContextAsync("employee", employee.Id))!.Data;

        data["Contract.City"].Should().Be("Km 5 Carretera a Masaya");
    }

    [Fact]
    public async Task MissingEmployee_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _employeeRepo.Setup(r => r.GetByIdWithBranchAsync(id)).ReturnsAsync((Employee?)null);

        var ctx = await CreateSut().GetEntityContextAsync("employee", id);

        ctx.Should().BeNull();
    }
}
