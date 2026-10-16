using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Warehouse;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Bodegas: CRUD con guards (nombre duplicado por sucursal, no borrar default
/// con herederas, no borrar bodega con productos), promoción a default,
/// <c>EnsureDefaultAsync</c> idempotente (garantía de "Bodega Principal") y
/// resolución de escritura.
/// </summary>
public sealed class WarehouseServiceTests
{
    private readonly Mock<IWarehouseRepository> _repo = new();
    private readonly Mock<IBranchValidator> _branchValidator = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly WarehouseService _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();

    public WarehouseServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);
        _branchValidator.Setup(v => v.ResolveForWriteAsync(It.IsAny<Guid?>()))
            .ReturnsAsync((Guid? b) => b);
        _sut = new WarehouseService(_repo.Object, _branchValidator.Object, _tenant.Object);
    }

    private Warehouse Make(
        Guid? id = null,
        Guid? branchId = null,
        string name = "Bodega Principal",
        bool isDefault = true,
        bool isActive = true) => new()
    {
        Id = id ?? Guid.NewGuid(),
        BranchId = branchId ?? _branchA,
        Name = name,
        IsActive = isActive,
        IsDefault = isDefault,
        TenantId = _companyId.ToString(),
        CompanyId = _companyId,
    };

    // ── Create ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithoutName_Throws()
    {
        var act = async () => await _sut.CreateAsync(new CreateWarehouseRequest(null, "  ", null, null, null));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*nombre*obligatorio*");
    }

    [Fact]
    public async Task Create_WithoutBranch_Throws()
    {
        _branchValidator.Setup(v => v.ResolveForWriteAsync(null)).ReturnsAsync((Guid?)null);

        var act = async () => await _sut.CreateAsync(new CreateWarehouseRequest(null, "Bodega Norte", null, null, null));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*sucursal*");
        _repo.Verify(r => r.AddAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task Create_DuplicateNameInBranch_Throws()
    {
        _repo.Setup(r => r.ExistsNameAsync(_branchA, "Bodega Norte", null)).ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(
            new CreateWarehouseRequest(_branchA, "Bodega Norte", null, null, null));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*ya existe*");
    }

    [Fact]
    public async Task Create_FirstWarehouseOfBranch_IsDefault()
    {
        _repo.Setup(r => r.CountByBranchAsync(_branchA)).ReturnsAsync(0);

        var result = await _sut.CreateAsync(
            new CreateWarehouseRequest(_branchA, "Bodega Norte", "BN-01", "Calle 1", null));

        result.IsDefault.Should().BeTrue("la primera bodega de la sucursal queda como default");
        result.IsActive.Should().BeTrue();
        result.BranchId.Should().Be(_branchA);

        _repo.Verify(r => r.AddAsync(It.Is<Warehouse>(w =>
            w.Name == "Bodega Norte" &&
            w.Code == "BN-01" &&
            w.Address == "Calle 1" &&
            w.IsDefault)), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_ExplicitDefault_DemotesOtherDefaults()
    {
        var incumbent = Make(name: "Vieja Default", isDefault: true);
        _repo.Setup(r => r.CountByBranchAsync(_branchA)).ReturnsAsync(1);
        _repo.Setup(r => r.GetFilteredAsync(null, _branchA, null, 1, 1000))
            .ReturnsAsync((new List<Warehouse> { incumbent }, 1));

        var result = await _sut.CreateAsync(
            new CreateWarehouseRequest(_branchA, "Nueva Default", null, null, true));

        result.IsDefault.Should().BeTrue();
        incumbent.IsDefault.Should().BeFalse("la default anterior debe deponerse");
        _repo.Verify(r => r.UpdateAsync(incumbent), Times.Once);
    }

    // ── Update ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Unknown_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Warehouse?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateWarehouseRequest(null, null, null, null));

        result.Should().BeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Update_RenameToNameOfAnotherWarehouse_Throws()
    {
        var existing = Make(name: "Bodega A");
        _repo.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        _repo.Setup(r => r.ExistsNameAsync(_branchA, "Bodega B", existing.Id)).ReturnsAsync(true);

        var act = async () => await _sut.UpdateAsync(existing.Id, new UpdateWarehouseRequest("Bodega B", null, null, null));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*ya existe*");
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var existing = Make(name: "Bodega A", isDefault: true);
        _repo.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(existing.Id,
            new UpdateWarehouseRequest("Bodega A Norte", "BA-N", "Av. 1", false));

        result!.Name.Should().Be("Bodega A Norte");
        result.Code.Should().Be("BA-N");
        result.Address.Should().Be("Av. 1");
        result.IsActive.Should().BeFalse();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── Delete ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Unknown_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Warehouse?)null);

        (await _sut.DeleteAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WithProducts_Throws()
    {
        var warehouse = Make(name: "Bodega A");
        _repo.Setup(r => r.GetByIdAsync(warehouse.Id)).ReturnsAsync(warehouse);
        _repo.Setup(r => r.CountProductsAsync(warehouse.Id)).ReturnsAsync(3);

        var act = async () => await _sut.DeleteAsync(warehouse.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*3 producto*Desactívela*");
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task Delete_DefaultWithOtherWarehouses_Throws()
    {
        var warehouse = Make(name: "Bodega A", isDefault: true);
        _repo.Setup(r => r.GetByIdAsync(warehouse.Id)).ReturnsAsync(warehouse);
        _repo.Setup(r => r.CountProductsAsync(warehouse.Id)).ReturnsAsync(0);
        _repo.Setup(r => r.CountByBranchAsync(_branchA)).ReturnsAsync(2);

        var act = async () => await _sut.DeleteAsync(warehouse.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*defecto*");
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task Delete_SoleDefaultWithNoProducts_Deletes()
    {
        var warehouse = Make(name: "Bodega A", isDefault: true);
        _repo.Setup(r => r.GetByIdAsync(warehouse.Id)).ReturnsAsync(warehouse);
        _repo.Setup(r => r.CountProductsAsync(warehouse.Id)).ReturnsAsync(0);
        _repo.Setup(r => r.CountByBranchAsync(_branchA)).ReturnsAsync(1);

        (await _sut.DeleteAsync(warehouse.Id)).Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(warehouse), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── SetDefault ───────────────────────────────────────────────────────

    [Fact]
    public async Task SetDefault_Unknown_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Warehouse?)null);

        var act = async () => await _sut.SetDefaultAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SetDefault_Inactive_Throws()
    {
        var warehouse = Make(name: "Bodega A", isDefault: false, isActive: false);
        _repo.Setup(r => r.GetByIdAsync(warehouse.Id)).ReturnsAsync(warehouse);

        var act = async () => await _sut.SetDefaultAsync(warehouse.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*inactiva*");
    }

    [Fact]
    public async Task SetDefault_ActiveWarehouse_DemotesOthersAndPromotes()
    {
        var incumbent = Make(name: "Vieja", isDefault: true);
        var candidate = Make(name: "Nueva", isDefault: false);
        _repo.Setup(r => r.GetByIdAsync(candidate.Id)).ReturnsAsync(candidate);
        _repo.Setup(r => r.GetFilteredAsync(null, _branchA, null, 1, 1000))
            .ReturnsAsync((new List<Warehouse> { incumbent, candidate }, 2));

        var result = await _sut.SetDefaultAsync(candidate.Id);

        result.IsDefault.Should().BeTrue();
        incumbent.IsDefault.Should().BeFalse();
    }

    // ── EnsureDefaultAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EnsureDefault_ExistingDefault_ReturnsIt_WithoutCreating()
    {
        var existing = Make(name: "Bodega Centro", isDefault: true);
        _repo.Setup(r => r.GetDefaultAsync(_branchA)).ReturnsAsync(existing);

        var result = await _sut.EnsureDefaultAsync(_branchA);

        result.Id.Should().Be(existing.Id);
        _repo.Verify(r => r.AddAsync(It.IsAny<Warehouse>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task EnsureDefault_WarehousesWithoutDefault_PromotesFirstActive()
    {
        var a = Make(name: "Bodega A", isDefault: false, isActive: false);
        var b = Make(name: "Bodega B", isDefault: false, isActive: true);
        _repo.Setup(r => r.GetDefaultAsync(_branchA)).ReturnsAsync((Warehouse?)null);
        _repo.Setup(r => r.GetByBranchAsync(_branchA)).ReturnsAsync(new List<Warehouse> { a, b });
        _repo.Setup(r => r.GetFilteredAsync(null, _branchA, null, 1, 1000))
            .ReturnsAsync((new List<Warehouse> { a, b }, 2));

        var result = await _sut.EnsureDefaultAsync(_branchA);

        result.Id.Should().Be(b.Id, "se promueve la primera ACTIVA");
        b.IsDefault.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task EnsureDefault_NoWarehouses_CreatesBodegaPrincipal()
    {
        _repo.Setup(r => r.GetDefaultAsync(_branchA)).ReturnsAsync((Warehouse?)null);
        _repo.Setup(r => r.GetByBranchAsync(_branchA)).ReturnsAsync(new List<Warehouse>());

        var result = await _sut.EnsureDefaultAsync(_branchA);

        result.Name.Should().Be("Bodega Principal");
        result.IsDefault.Should().BeTrue();
        result.BranchId.Should().Be(_branchA);
        _repo.Verify(r => r.AddAsync(It.Is<Warehouse>(w => w.Name == "Bodega Principal")), Times.Once);
    }

    // ── ResolveForWriteAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ResolveForWrite_ExplicitValidWarehouse_ReturnsIt()
    {
        var warehouse = Make(name: "Bodega A");
        _repo.Setup(r => r.GetByIdAsync(warehouse.Id)).ReturnsAsync(warehouse);

        var result = await _sut.ResolveForWriteAsync(warehouse.Id, _branchA);

        result.Id.Should().Be(warehouse.Id);
    }

    [Fact]
    public async Task ResolveForWrite_UnknownWarehouse_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Warehouse?)null);

        var act = async () => await _sut.ResolveForWriteAsync(Guid.NewGuid(), _branchA);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*no existe*");
    }

    [Fact]
    public async Task ResolveForWrite_WarehouseOfAnotherBranch_Throws()
    {
        var warehouse = Make(name: "Bodega B", branchId: _branchB);
        _repo.Setup(r => r.GetByIdAsync(warehouse.Id)).ReturnsAsync(warehouse);

        var act = async () => await _sut.ResolveForWriteAsync(warehouse.Id, _branchA);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*no pertenece*");
    }

    [Fact]
    public async Task ResolveForWrite_InactiveWarehouse_Throws()
    {
        var warehouse = Make(name: "Bodega A", isActive: false);
        _repo.Setup(r => r.GetByIdAsync(warehouse.Id)).ReturnsAsync(warehouse);

        var act = async () => await _sut.ResolveForWriteAsync(warehouse.Id, _branchA);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*inactiva*");
    }

    [Fact]
    public async Task ResolveForWrite_Null_FallsBackToEnsureDefault()
    {
        var existing = Make(name: "Bodega Centro", isDefault: true);
        _repo.Setup(r => r.GetDefaultAsync(_branchA)).ReturnsAsync(existing);

        var result = await _sut.ResolveForWriteAsync(null, _branchA);

        result.Id.Should().Be(existing.Id);
    }
}
