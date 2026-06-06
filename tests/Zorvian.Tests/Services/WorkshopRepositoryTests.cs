using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

public sealed class WorkshopRepositoryTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly ServiceWorkshopRepository _sut;

    public WorkshopRepositoryTests()
    {
        var tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new ServiceWorkshopRepository(_db);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllForCompany()
    {
        var companyId = Guid.NewGuid();
        _db.ServiceWorkshops.AddRange(
            new ServiceWorkshop { Id = Guid.NewGuid(), Code = "WS001", Name = "Workshop A", CompanyId = companyId, TenantId = _tenant.Object.TenantId.ToString(), BranchId = Guid.NewGuid(), CreatedBy = "test" },
            new ServiceWorkshop { Id = Guid.NewGuid(), Code = "WS002", Name = "Workshop B", CompanyId = companyId, TenantId = _tenant.Object.TenantId.ToString(), BranchId = Guid.NewGuid(), CreatedBy = "test" }
        );
        await _db.SaveChangesAsync();

        var results = await _sut.GetAllAsync(companyId);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsWorkshop()
    {
        var workshop = new ServiceWorkshop { Id = Guid.NewGuid(), Code = "WS003", Name = "Workshop C", CompanyId = Guid.NewGuid(), TenantId = _tenant.Object.TenantId.ToString(), BranchId = Guid.NewGuid(), CreatedBy = "test" };

        await _sut.AddAsync(workshop);
        await _sut.SaveChangesAsync();

        var saved = await _db.ServiceWorkshops.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == workshop.Id);
        Assert.NotNull(saved);
    }
}
