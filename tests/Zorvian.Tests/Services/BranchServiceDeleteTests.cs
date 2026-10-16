using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Sucursal: política de borrado seguro. No se elimina una sucursal con
/// productos activos, historial de ventas o compras, ni empleados asignados;
/// si pasa las guardas, sus bodegas se dan de baja en cascada (soft-delete)
/// junto con la sucursal.
/// </summary>
public sealed class BranchServiceDeleteTests
{
    private readonly Mock<IBranchRepository> _branchRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<IPurchaseRepository> _purchaseRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IBranchValidator> _branchValidator = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly BranchService _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();

    public BranchServiceDeleteTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);

        // WarehouseService real (sealed) con repo/mock del validador.
        var warehouseService = new WarehouseService(
            _warehouseRepo.Object, _branchValidator.Object, _tenant.Object);

        _sut = new BranchService(
            _branchRepo.Object,
            warehouseService,
            _productRepo.Object,
            _saleRepo.Object,
            _purchaseRepo.Object,
            _employeeRepo.Object,
            _tenant.Object,
            _mapper.Object);

        // Sin dependencias por defecto: todas las guardas en 0.
        _productRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(0);
        _saleRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid>()))
            .ReturnsAsync(0);
        _purchaseRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid>()))
            .ReturnsAsync(0);
        _employeeRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid>()))
            .ReturnsAsync(0);
        _warehouseRepo.Setup(r => r.GetByBranchAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Warehouse>());
    }

    private Branch MakeBranch() => new()
    {
        Id = _branchId,
        Name = "Sucursal Central",
        CompanyId = _companyId,
        TenantId = _companyId.ToString(),
        IsActive = true,
    };

    private void SetupBranch() =>
        _branchRepo.Setup(r => r.GetByIdAsync(_branchId)).ReturnsAsync(MakeBranch());

    // ── Guardas ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_UnknownBranch_ReturnsFalse_NoDeletes()
    {
        _branchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Branch?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
        _branchRepo.Verify(r => r.DeleteAsync(It.IsAny<Branch>()), Times.Never);
        _warehouseRepo.Verify(r => r.DeleteAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WithActiveProducts_Throws_MessagesCount()
    {
        SetupBranch();
        _productRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<bool?>(), true, _branchId, It.IsAny<Guid?>()))
            .ReturnsAsync(3);

        var act = async () => await _sut.DeleteAsync(_branchId);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*3 producto(s) activo(s)*");
        _branchRepo.Verify(r => r.DeleteAsync(It.IsAny<Branch>()), Times.Never);
        _warehouseRepo.Verify(r => r.DeleteAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WithSalesHistory_Throws()
    {
        SetupBranch();
        _saleRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), _branchId))
            .ReturnsAsync(12);

        var act = async () => await _sut.DeleteAsync(_branchId);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*12 venta(s)*");
        _branchRepo.Verify(r => r.DeleteAsync(It.IsAny<Branch>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WithPurchaseHistory_Throws()
    {
        SetupBranch();
        _purchaseRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), _branchId))
            .ReturnsAsync(5);

        var act = async () => await _sut.DeleteAsync(_branchId);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*5 compra(s)*");
        _branchRepo.Verify(r => r.DeleteAsync(It.IsAny<Branch>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WithAssignedEmployees_Throws()
    {
        SetupBranch();
        _employeeRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), _branchId))
            .ReturnsAsync(2);

        var act = async () => await _sut.DeleteAsync(_branchId);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*2 empleado(s)*");
        _branchRepo.Verify(r => r.DeleteAsync(It.IsAny<Branch>()), Times.Never);
    }

    // ── Cascada y borrado válido ─────────────────────────────────────────

    [Fact]
    public async Task Delete_EmptyBranch_CascadesWarehouseSoftDelete()
    {
        SetupBranch();
        var w1 = new Warehouse { Id = Guid.NewGuid(), BranchId = _branchId, Name = "Bodega Principal" };
        var w2 = new Warehouse { Id = Guid.NewGuid(), BranchId = _branchId, Name = "Bodega Norte" };
        _warehouseRepo.Setup(r => r.GetByBranchAsync(_branchId)).ReturnsAsync(new List<Warehouse> { w1, w2 });

        var result = await _sut.DeleteAsync(_branchId);

        result.Should().BeTrue();
        _warehouseRepo.Verify(r => r.DeleteAsync(w1), Times.Once);
        _warehouseRepo.Verify(r => r.DeleteAsync(w2), Times.Once);
        _warehouseRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _branchRepo.Verify(r => r.DeleteAsync(It.IsAny<Branch>()), Times.Once);
        _branchRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Delete_EmptyBranchWithoutWarehouses_StillDeletes_NoWarehouseSave()
    {
        SetupBranch();

        var result = await _sut.DeleteAsync(_branchId);

        result.Should().BeTrue();
        _warehouseRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _branchRepo.Verify(r => r.DeleteAsync(It.IsAny<Branch>()), Times.Once);
    }

    [Fact]
    public async Task Delete_BlockingGuardShortCircuits_SkipsLaterChecks()
    {
        // Productos activos presentes: no debe ni consultar ventas/compras/empleados.
        SetupBranch();
        _productRepo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(1);

        var act = async () => await _sut.DeleteAsync(_branchId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _saleRepo.Verify(r => r.GetFilteredCountAsync(
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid>()), Times.Never);
        _purchaseRepo.Verify(r => r.GetFilteredCountAsync(
            It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid>()), Times.Never);
        _employeeRepo.Verify(r => r.GetFilteredCountAsync(
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid>()), Times.Never);
    }
}
