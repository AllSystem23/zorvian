using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Matriz de visibilidad por sucursal del PurchaseOrderRepository.
/// Regresión del bug: el repo aplicaba .Where(o => o.BranchId == branchId) sin
/// guardia, así que con Guid.Empty ("todas") solo devolvía ÓCs sin sucursal.
/// </summary>
public sealed class PurchaseOrderRepositoryTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly PurchaseOrderRepository _sut;
    private readonly string _tenantId;
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();

    public PurchaseOrderRepositoryTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new PurchaseOrderRepository(_db);

        SeedOrder(_branchA, "OC-A");
        SeedOrder(_branchB, "OC-B");
    }

    private void SeedOrder(Guid branchId, string number)
    {
        var supplierId = Guid.NewGuid();
        _db.Suppliers.Add(new Supplier
        {
            Id = supplierId,
            TenantId = _tenantId,
            CompanyId = Guid.NewGuid(),
            Code = $"S{supplierId.ToString()[..6]}",
            Name = "Test Supplier",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _db.PurchaseOrders.Add(new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = Guid.NewGuid(),
            BranchId = branchId,
            OrderNumber = number,
            SupplierId = supplierId,
            OrderDate = DateTime.UtcNow,
            Status = "draft",
            Total = 500
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchVacio_DevuelveTodasLasSucursales()
    {
        var (items, total) = await _sut.GetFilteredAsync(null, null, null, null, null, Guid.Empty, 1, 20);

        Assert.Equal(2, items.Count);
        Assert.Equal(2, total);
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchConcreto_DevuelveSoloEsaSucursal()
    {
        var (items, total) = await _sut.GetFilteredAsync(null, null, null, null, null, _branchA, 1, 20);

        Assert.Single(items);
        Assert.Equal(_branchA, items[0].BranchId);
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchSinDatos_DevuelveVacio()
    {
        var (items, total) = await _sut.GetFilteredAsync(null, null, null, null, null, Guid.NewGuid(), 1, 20);

        Assert.Empty(items);
        Assert.Equal(0, total);
    }
}
