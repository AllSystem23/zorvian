using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Infrastructure;

public sealed class SoftDeleteIntegrationTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly AuditInterceptor _auditInterceptor;

    public SoftDeleteIntegrationTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _tenant.Setup(t => t.CurrentUserId).Returns((Guid?)null);
        _auditInterceptor = new AuditInterceptor(_tenant.Object);
    }

    private ZorvianDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(_auditInterceptor)
            .Options;

        return new ZorvianDbContext(options, _tenant.Object);
    }

    [Fact]
    public async Task SoftDeletedEntity_NotReturnedByDefault()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var tenantId = _tenant.Object.TenantId;
        var active = new Company { Id = Guid.NewGuid(), Name = "Active Corp", TaxId = "111", TenantId = tenantId };
        var deleted = new Company { Id = Guid.NewGuid(), Name = "Deleted Corp", TaxId = "222", TenantId = tenantId, IsDeleted = true, DeletedAt = DateTime.UtcNow };

        db.Companies.Add(active);
        db.Companies.Add(deleted);
        await db.SaveChangesAsync();

        var companies = await db.Companies.ToListAsync();

        Assert.Single(companies);
        Assert.Equal("Active Corp", companies[0].Name);
    }

    [Fact]
    public async Task IgnoreQueryFilters_ReturnsSoftDeletedEntities()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var tenantId = _tenant.Object.TenantId;
        var deleted = new Company { Id = Guid.NewGuid(), Name = "Deleted Corp", TaxId = "222", TenantId = tenantId, IsDeleted = true, DeletedAt = DateTime.UtcNow };

        db.Companies.Add(deleted);
        await db.SaveChangesAsync();

        var companies = await db.Companies.IgnoreQueryFilters().ToListAsync();

        Assert.Contains(companies, c => c.Name == "Deleted Corp");
    }

    [Fact]
    public async Task RemoveEntity_SetsIsDeletedAndDeletedAt()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var tenantId = _tenant.Object.TenantId;
        var emp = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Juan",
            LastName = "Perez",
            Email = "juan@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
            TenantId = tenantId,
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        db.Employees.Remove(emp);
        await db.SaveChangesAsync();

        var softDeleted = await db.Employees.IgnoreQueryFilters().FirstAsync(e => e.Id == emp.Id);

        Assert.True(softDeleted.IsDeleted);
        Assert.NotNull(softDeleted.DeletedAt);
    }

    [Fact]
    public async Task SoftDelete_DoesNotAffectOtherTenants()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        // Seed data: Tenant A has 2 companies, Tenant B has 1
        var tenantA = Guid.NewGuid().ToString();
        var tenantB = Guid.NewGuid().ToString();
        var companyA1 = new Company { Id = Guid.NewGuid(), Name = "TenantA Active", TaxId = "1", TenantId = tenantA };
        var companyA2 = new Company { Id = Guid.NewGuid(), Name = "TenantA Deleted", TaxId = "2", TenantId = tenantA, IsDeleted = true, DeletedAt = DateTime.UtcNow };
        var companyB1 = new Company { Id = Guid.NewGuid(), Name = "TenantB Active", TaxId = "3", TenantId = tenantB };

        db.Companies.AddRange(companyA1, companyA2, companyB1);
        await db.SaveChangesAsync();

        // Query as Tenant A - should only see active companies
        _tenant.Setup(t => t.TenantId).Returns(TenantId.FromString(tenantA));
        var visibleA = await db.Companies.ToListAsync();

        Assert.Single(visibleA);
        Assert.Equal("TenantA Active", visibleA[0].Name);
    }
}
