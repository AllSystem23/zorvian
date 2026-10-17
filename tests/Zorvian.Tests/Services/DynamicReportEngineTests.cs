using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

/// <summary>
/// Fase B del motor dinámico: (1) filtros gt/lt sobre FECHAS funcionan (antes
/// CompareNumeric solo parseaba decimals y las fechas devolvían siempre 0 → el
/// filtro no excluía nada ni filtraba); (2) las filas exponen branchId/branchName;
/// (3) el scope de sucursal llega a los loaders (con Guid.Empty = todas).
/// </summary>
public sealed class DynamicReportEngineTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BranchA = Guid.NewGuid();

    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<IPurchaseRepository> _purchaseRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IAccountingEntryRepository> _entryRepo = new();
    private readonly Mock<IClientRepository> _clientRepo = new();
    private readonly Mock<ISupplierRepository> _supplierRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();
    private readonly DynamicReportEngine _sut;

    public DynamicReportEngineTests()
    {
        _sut = new DynamicReportEngine(
            _saleRepo.Object, _purchaseRepo.Object, _productRepo.Object, _entryRepo.Object,
            _clientRepo.Object, _supplierRepo.Object, _employeeRepo.Object, _branchRepo.Object);

        _branchRepo.Setup(r => r.GetAllAsync(CompanyId)).ReturnsAsync(new List<Branch>());
    }

    private static Sale SaleAt(DateTime date, string invoice, Guid branchId) => new()
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = invoice,
        SaleDate = date,
        BranchId = branchId,
        Total = 100,
        Status = "completed",
    };

    private void SetupSales(params Sale[] sales) =>
        _saleRepo.Setup(r => r.GetFilteredAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(),
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(sales.ToList());

    [Fact]
    public async Task ExecuteAsync_FiltroGtSobreFecha_DevuelveSoloLasPosteriores()
    {
        var early = SaleAt(new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), "F-1", Guid.NewGuid());
        var late = SaleAt(new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), "F-2", Guid.NewGuid());
        SetupSales(early, late);

        var result = await _sut.ExecuteAsync(
            "sales",
            new List<ReportField>(),
            new List<ReportFilter> { new("saleDate", "gt", "2026-01-15", null) },
            null, null, "asc", CompanyId, Guid.Empty);

        // Antes del fix de CompareNumeric esto devolvía las 2 filas (comparación == 0).
        Assert.Single(result.Rows);
        Assert.Equal("F-2", result.Rows[0]["invoiceNumber"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_FiltroLtSobreFecha_DevuelveSoloLasAnteriores()
    {
        var early = SaleAt(new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), "F-1", Guid.NewGuid());
        var late = SaleAt(new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), "F-2", Guid.NewGuid());
        SetupSales(early, late);

        var result = await _sut.ExecuteAsync(
            "sales",
            new List<ReportField>(),
            new List<ReportFilter> { new("saleDate", "lt", "2026-01-15", null) },
            null, null, "asc", CompanyId, Guid.Empty);

        Assert.Single(result.Rows);
        Assert.Equal("F-1", result.Rows[0]["invoiceNumber"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_AgregaBranchIdYBranchNameALasFilas()
    {
        _branchRepo.Setup(r => r.GetAllAsync(CompanyId))
            .ReturnsAsync(new List<Branch> { new() { Id = BranchA, Name = "Central" } });
        SetupSales(SaleAt(new DateTime(2026, 1, 5), "F-1", BranchA));

        var result = await _sut.ExecuteAsync(
            "sales", new List<ReportField>(), new List<ReportFilter>(),
            null, null, "asc", CompanyId, BranchA);

        Assert.Single(result.Rows);
        Assert.Equal(BranchA, result.Rows[0]["branchId"]);
        Assert.Equal("Central", result.Rows[0]["branchName"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_PasaLaSucursalAlLoaderDeVentas()
    {
        SetupSales();

        await _sut.ExecuteAsync(
            "sales", new List<ReportField>(), new List<ReportFilter>(),
            null, null, "asc", CompanyId, BranchA);

        _saleRepo.Verify(r => r.GetFilteredAsync(
            null, null, null, null, null, null, BranchA, 1, int.MaxValue), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyBranch_LoaderDeEmpleadosRecibeNull_TodasLasSucursales()
    {
        _employeeRepo.Setup(r => r.GetFilteredAsync(null, null, null, 1, int.MaxValue, null))
            .ReturnsAsync(new List<Employee>());

        await _sut.ExecuteAsync(
            "employees", new List<ReportField>(), new List<ReportFilter>(),
            null, null, "asc", CompanyId, Guid.Empty);

        // EmployeeRepository usa Guid? (null = todas); el resto de loaders usan Guid.Empty.
        _employeeRepo.Verify(r => r.GetFilteredAsync(null, null, null, 1, int.MaxValue, null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_FiltroContainsSobreBranchName_FiltraPorNombreDeSucursal()
    {
        _branchRepo.Setup(r => r.GetAllAsync(CompanyId))
            .ReturnsAsync(new List<Branch>
            {
                new() { Id = BranchA, Name = "Sucursal Central" },
                new() { Id = Guid.NewGuid(), Name = "Sucursal Norte" },
            });
        SetupSales(
            SaleAt(new DateTime(2026, 1, 5), "F-1", BranchA),
            SaleAt(new DateTime(2026, 1, 5), "F-2", Guid.NewGuid()));

        var result = await _sut.ExecuteAsync(
            "sales",
            new List<ReportField>(),
            new List<ReportFilter> { new("branchName", "contains", "Central", null) },
            null, null, "asc", CompanyId, Guid.Empty);

        Assert.Single(result.Rows);
        Assert.Equal("F-1", result.Rows[0]["invoiceNumber"]?.ToString());
    }
}
