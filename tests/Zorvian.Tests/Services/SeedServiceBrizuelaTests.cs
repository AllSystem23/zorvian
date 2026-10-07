using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using AutoMapper;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

/// <summary>
/// Tests del seed idempotente de Tienda Brizuela Romero (SeedService.SeedBrizuelaRomeroAsync).
/// Usa repositorios reales sobre EF InMemory para verificar la idempotencia contra la "BD".
/// </summary>
public sealed class SeedServiceBrizuelaTests : IDisposable
{
    private const string RootCatalogCode = "1.00.00.000.0000";
    private const string SaleInvoiceTrigger = "SALE_INVOICE";

    private readonly ZorvianDbContext _db;
    private readonly TenantContext _tenantContext = new(); // Real: implementa ITenantContext + ITenantContextWriter
    private readonly Mock<IFiscalService> _fiscal = new();
    private readonly Mock<IFirebaseAuthService> _firebase = new();
    private readonly Mock<IAccountingRuleTemplateRepository> _templateRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly SeedService _seed;

    public SeedServiceBrizuelaTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ZorvianDbContext(options, _tenantContext);

        // Repositorios reales: la idempotencia depende de que las cuentas
        // importadas sean visibles en el DbContext, no de un mock.
        var accountRepo = new AccountRepository(_db);
        var entryRepo = new AccountingEntryRepository(_db);
        var linkRepo = new AccountLinkRepository(_db);
        // IMapper no se usa en las rutas que ejercita el seed; un mock es suficiente
        // y cualquier llamada inesperada fallaría el test con NullReference.
        var mapper = new Mock<IMapper>().Object;

        var accountService = new AccountService(accountRepo, entryRepo, _tenantContext, mapper, _companyRepo.Object);
        var linkService = new AccountLinkService(linkRepo, accountRepo, _tenantContext);

        _fiscal.Setup(f => f.SetupDefaultTaxesAsync(It.IsAny<Guid>(), It.IsAny<string>()))
               .Returns(Task.CompletedTask);

        _seed = new SeedService(_db, _firebase.Object, _fiscal.Object, accountService, linkService, _templateRepo.Object, _tenantContext);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SeedBrizuelaRomeroAsync_SuperAdminTenant_CreatesCompanyWithCatalogAndRules()
    {
        var result = await _seed.SeedBrizuelaRomeroAsync("superadmin", isSuperAdminCaller: true);

        Assert.True(result.CompanyCreated);
        Assert.Equal("Tienda Brizuela Romero", result.CompanyName);
        Assert.NotEqual("superadmin", result.TenantId);
        Assert.True(Guid.TryParse(result.TenantId, out _));

        var company = await _db.Companies.IgnoreQueryFilters()
            .SingleAsync(c => c.TenantId == result.TenantId);
        Assert.Equal(company.Id, result.CompanyId);
        Assert.Equal("Nicaragua", company.Country);
        Assert.Equal("NIO", company.Currency);

        _fiscal.Verify(f => f.SetupDefaultTaxesAsync(company.Id, "NIC"), Times.Once);

        // Catálogo de cuentas importado desde el CSV embebido
        var rootAccount = await _db.Accounts.IgnoreQueryFilters()
            .SingleAsync(a => a.Code == RootCatalogCode);
        Assert.Equal(result.TenantId, rootAccount.TenantId);

        // Regla de autocontabilización creada (sin duplicados)
        var template = await _db.AccountingRuleTemplates.IgnoreQueryFilters()
            .SingleAsync(t => t.CompanyId == company.Id);
        Assert.Equal(SaleInvoiceTrigger, template.ProcessTrigger);
        Assert.Equal("NIC", template.CountryCode);
        Assert.True(template.IsActive);
        Assert.Contains("AccountingRules", template.EntryStructureJson);
    }

    [Fact]
    public async Task SeedBrizuelaRomeroAsync_SuperAdminTenant_LinksSuperAdminUser()
    {
        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            FirebaseUid = "fb-test-superadmin",
            Email = "superadmin@test.com",
            DisplayName = "Super Admin",
            TenantId = "superadmin",
            IsActive = true,
        };
        _db.Users.Add(superAdmin);
        await _db.SaveChangesAsync();

        var result = await _seed.SeedBrizuelaRomeroAsync("superadmin", isSuperAdminCaller: true);

        var linked = await _db.UserTenants.IgnoreQueryFilters()
            .AnyAsync(ut => ut.UserId == superAdmin.Id && ut.TenantId == result.TenantId);
        Assert.True(linked);
    }

    [Fact]
    public async Task SeedBrizuelaRomeroAsync_SecondRunIsIdempotent()
    {
        var first = await _seed.SeedBrizuelaRomeroAsync("superadmin", isSuperAdminCaller: true);
        var second = await _seed.SeedBrizuelaRomeroAsync("superadmin", isSuperAdminCaller: true);

        // Reutiliza la compañía creada en la primera corrida
        Assert.False(second.CompanyCreated);
        Assert.Equal(first.TenantId, second.TenantId);
        Assert.Equal(first.CompanyId, second.CompanyId);

        // No re-importa catálogo ni reglas
        Assert.False(second.AccountsImported);
        Assert.False(second.RulesImported);

        Assert.Single(await _db.Companies.IgnoreQueryFilters()
            .Where(c => c.Name == "Tienda Brizuela Romero").ToListAsync());
        Assert.Single(await _db.Accounts.IgnoreQueryFilters()
            .Where(a => a.Code == RootCatalogCode).ToListAsync());
        Assert.Single(await _db.AccountingRuleTemplates.IgnoreQueryFilters()
            .Where(t => t.ProcessTrigger == SaleInvoiceTrigger).ToListAsync());
    }

    [Fact]
    public async Task SeedBrizuelaRomeroAsync_CompanyTenant_ImportsIntoExistingCompany()
    {
        var tenantId = Guid.NewGuid().ToString();
        var company = new Company
        {
            Name = "Mi Comercio",
            LegalName = "Mi Comercio",
            TaxId = "X0010101010",
            Country = "Nicaragua",
            Currency = "NIO",
            Timezone = "America/Managua",
            MaxEmployees = 10,
            TenantId = tenantId,
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        var result = await _seed.SeedBrizuelaRomeroAsync(tenantId, isSuperAdminCaller: false);

        Assert.False(result.CompanyCreated);
        Assert.Equal(company.Id, result.CompanyId);
        Assert.Equal(tenantId, result.TenantId);
        Assert.True(result.AccountsImported);
        Assert.True(result.RulesImported);

        var rootAccount = await _db.Accounts.IgnoreQueryFilters()
            .SingleAsync(a => a.Code == RootCatalogCode);
        Assert.Equal(tenantId, rootAccount.TenantId);

        var template = await _db.AccountingRuleTemplates.IgnoreQueryFilters()
            .SingleAsync(t => t.CompanyId == company.Id);
        Assert.Equal(SaleInvoiceTrigger, template.ProcessTrigger);
    }

    [Fact]
    public async Task SeedBrizuelaRomeroAsync_GuidTenantWithoutCompany_CreatesCompanyUnderSameTenant()
    {        var tenantId = Guid.NewGuid().ToString();

        var result = await _seed.SeedBrizuelaRomeroAsync(tenantId, isSuperAdminCaller: false);

        Assert.True(result.CompanyCreated);
        Assert.Equal(tenantId, result.TenantId);

        var company = await _db.Companies.IgnoreQueryFilters()
            .SingleAsync(c => c.TenantId == tenantId);
        Assert.Equal("Tienda Brizuela Romero", company.Name);
        Assert.Equal(company.Id, result.CompanyId);

        var rootAccount = await _db.Accounts.IgnoreQueryFilters()
            .SingleAsync(a => a.Code == RootCatalogCode);
        Assert.Equal(tenantId, rootAccount.TenantId);
    }

    [Fact]
    public async Task SeedBrizuelaRomeroAsync_SuperAdminAutoSelectedToOtherCompany_CreatesBrizuelaInsteadOfContaminating()
    {
        // Simula lo que hace TenantMiddleware: SuperAdmin con otra empresa auto-cargada
        var otherTenantId = Guid.NewGuid().ToString();
        var otherCompany = new Company
        {
            Name = "Otra Compañía",
            LegalName = "Otra Compañía",
            TaxId = "X9999999999",
            Country = "Nicaragua",
            Currency = "NIO",
            Timezone = "America/Managua",
            MaxEmployees = 10,
            TenantId = otherTenantId,
        };
        _db.Companies.Add(otherCompany);
        await _db.SaveChangesAsync();

        var result = await _seed.SeedBrizuelaRomeroAsync(otherTenantId, isSuperAdminCaller: true);

        // NO debe importar el catálogo a la compañía equivocada: crea una nueva para Brizuela
        Assert.True(result.CompanyCreated);
        Assert.NotEqual(otherCompany.Id, result.CompanyId);
        Assert.Equal("Tienda Brizuela Romero", result.CompanyName);
        Assert.NotEqual(otherTenantId, result.TenantId);

        // La otra compañía queda intacta (sin catálogo de Brizuela)
        var otherHasCatalog = await _db.Accounts.IgnoreQueryFilters()
            .AnyAsync(a => a.TenantId == otherTenantId);
        Assert.False(otherHasCatalog);

        // Y la nueva compañía Brizuela queda bajo el tenant nuevo
        var brizuela = await _db.Companies.IgnoreQueryFilters()
            .SingleAsync(c => c.Id == result.CompanyId);
        Assert.Equal("Tienda Brizuela Romero", brizuela.Name);
        var rootAccount = await _db.Accounts.IgnoreQueryFilters()
            .SingleAsync(a => a.Code == RootCatalogCode);
        Assert.Equal(result.TenantId, rootAccount.TenantId);
    }

    [Fact]
    public async Task SeedBrizuelaRomeroAsync_NonSuperAdminOnOtherCompany_ImportsIntoIt()
    {
        // Usuario normal autenticado con su compañía (no SuperAdmin): importar ahí es correcto
        var tenantId = Guid.NewGuid().ToString();
        var company = new Company
        {
            Name = "Compañía Del Usuario",
            LegalName = "Compañía Del Usuario",
            TaxId = "X8888888888",
            Country = "Nicaragua",
            Currency = "NIO",
            Timezone = "America/Managua",
            MaxEmployees = 10,
            TenantId = tenantId,
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        var result = await _seed.SeedBrizuelaRomeroAsync(tenantId, isSuperAdminCaller: false);

        Assert.False(result.CompanyCreated);
        Assert.Equal(company.Id, result.CompanyId);
        Assert.True(result.AccountsImported);
        Assert.True(result.RulesImported);
    }
}
