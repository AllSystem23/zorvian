using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class BranchValidatorTests
{
    private readonly Mock<IBranchRepository> _branchRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly BranchValidator _sut;
    private readonly Guid _companyId = Guid.NewGuid();

    public BranchValidatorTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(new TenantId(_companyId));
        _tenant.Setup(t => t.SelectedCompanyId).Returns(_companyId);
        _tenant.Setup(t => t.HasCompanySelection).Returns(true);
        _tenant.Setup(t => t.IsSuperAdmin).Returns(false);
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);
        _sut = new BranchValidator(_branchRepo.Object, _tenant.Object);
    }

    [Fact]
    public async Task SinBranchId_YConSucursalEnContexto_DevuelveSucursalDelContexto()
    {
        var contextBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);

        var result = await _sut.ResolveForWriteAsync(null);

        Assert.Equal(contextBranch, result);
    }

    [Fact]
    public async Task SinBranchId_YSinSucursalEnContexto_DevuelveNull()
    {
        var result = await _sut.ResolveForWriteAsync(null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ConBranchIdVacio_DevuelveSucursalDelContexto()
    {
        var contextBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);

        var result = await _sut.ResolveForWriteAsync(Guid.Empty);

        Assert.Equal(contextBranch, result);
    }

    [Fact]
    public async Task ConBranchIdPertenecienteALaCompania_DevuelveElBranchId()
    {
        var branchId = Guid.NewGuid();
        _branchRepo.Setup(r => r.GetByIdAsync(branchId))
            .ReturnsAsync(new Branch { Id = branchId, CompanyId = _companyId, Name = "Central" });

        var result = await _sut.ResolveForWriteAsync(branchId);

        Assert.Equal(branchId, result);
    }

    [Fact]
    public async Task ConBranchIdDeOtraCompania_LanzaExcepcion()
    {
        var branchId = Guid.NewGuid();
        _branchRepo.Setup(r => r.GetByIdAsync(branchId))
            .ReturnsAsync(new Branch { Id = branchId, CompanyId = Guid.NewGuid(), Name = "Ajena" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ResolveForWriteAsync(branchId));

        Assert.Contains("no pertenece a la compa", ex.Message);
    }

    [Fact]
    public async Task ConBranchIdInexistente_LanzaExcepcion()
    {
        var branchId = Guid.NewGuid();
        _branchRepo.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync((Branch?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ResolveForWriteAsync(branchId));
    }

    [Fact]
    public async Task SuperAdminSinCompania_DevuelveElBranchIdSinValidar()
    {
        _tenant.Setup(t => t.TenantId).Returns(TenantId.FromString("superadmin"));
        _tenant.Setup(t => t.SelectedCompanyId).Returns((Guid?)null);
        _tenant.Setup(t => t.HasCompanySelection).Returns(false);
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);

        var branchId = Guid.NewGuid();

        var result = await _sut.ResolveForWriteAsync(branchId);

        Assert.Equal(branchId, result);
        _branchRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task SinCompania_YNoSuperAdmin_LanzaExcepcion()
    {
        _tenant.Setup(t => t.TenantId).Returns(new TenantId(Guid.Empty));
        _tenant.Setup(t => t.SelectedCompanyId).Returns((Guid?)null);
        _tenant.Setup(t => t.HasCompanySelection).Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ResolveForWriteAsync(Guid.NewGuid()));
    }
}
