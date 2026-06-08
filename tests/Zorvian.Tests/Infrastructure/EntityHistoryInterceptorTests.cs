using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Infrastructure;

public sealed class EntityHistoryInterceptorTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly EntityHistoryInterceptor _interceptor;

    public EntityHistoryInterceptorTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _tenant.Setup(t => t.CurrentUserId).Returns((Guid?)null);
        _interceptor = new EntityHistoryInterceptor();
    }

    private ZorvianDbContext CreateDb(string dbName)
    {
        var auditInterceptor = new AuditInterceptor(_tenant.Object);
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(_interceptor, auditInterceptor)
            .Options;

        return new ZorvianDbContext(options, _tenant.Object);
    }

    [Fact]
    public async Task SaveChanges_AddedEntity_CreatesEntityHistory()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Juan",
            LastName = "Perez",
            Email = "juan@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
            TenantId = "tenant-1",
        });

        await db.SaveChangesAsync();

        var history = await db.EntityHistories.IgnoreQueryFilters()
            .Where(h => h.ChangeType == "Create")
            .ToListAsync();

        Assert.NotEmpty(history);
        Assert.All(history, h => Assert.Equal("Create", h.ChangeType));
        Assert.Contains(history, h => h.FieldName == "FirstName" && h.NewValue == "Juan");
        Assert.Contains(history, h => h.FieldName == "LastName" && h.NewValue == "Perez");
    }

    [Fact]
    public async Task SaveChanges_ModifiedEntity_CreatesFieldLevelHistory()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var empId = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = empId,
            FirstName = "Juan",
            LastName = "Perez",
            Email = "juan@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var emp = await db.Employees.IgnoreQueryFilters().FirstAsync(e => e.Id == empId);
        emp.FirstName = "Pedro";
        emp.Email = "pedro@test.com";
        await db.SaveChangesAsync();

        var updates = await db.EntityHistories.IgnoreQueryFilters()
            .Where(h => h.ChangeType == "Update" && h.EntityId == empId)
            .ToListAsync();

        Assert.Contains(updates, h => h.FieldName == "FirstName" && h.OldValue == "Juan" && h.NewValue == "Pedro");
        Assert.Contains(updates, h => h.FieldName == "Email" && h.OldValue == "juan@test.com" && h.NewValue == "pedro@test.com");
    }

    [Fact]
    public async Task SaveChanges_DeletedEntity_CreatesDeleteHistory()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var empId = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = empId,
            FirstName = "Juan",
            LastName = "Perez",
            Email = "juan@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var emp = await db.Employees.IgnoreQueryFilters().FirstAsync(e => e.Id == empId);
        db.Employees.Remove(emp);
        await db.SaveChangesAsync();

        var deleted = await db.EntityHistories.IgnoreQueryFilters()
            .Where(h => h.ChangeType == "Delete" && h.EntityId == empId)
            .ToListAsync();

        Assert.NotEmpty(deleted);
        Assert.Contains(deleted, h => h.FieldName == "*");
    }

    [Fact]
    public async Task SaveChanges_EntityHistoryItself_NotLogged()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.EntityHistories.Add(new EntityHistory
        {
            EntityType = "Test",
            EntityId = Guid.NewGuid(),
            FieldName = "Name",
            ChangeType = "Create",
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var count = await db.EntityHistories.IgnoreQueryFilters().CountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SaveChanges_ExcludedProperties_NotLogged()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "Test Corp",
            TaxId = "123",
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var history = await db.EntityHistories.IgnoreQueryFilters()
            .Where(h => h.ChangeType == "Create")
            .ToListAsync();

        Assert.DoesNotContain(history, h => h.FieldName == "Id");
        Assert.DoesNotContain(history, h => h.FieldName == "TenantId");
        Assert.DoesNotContain(history, h => h.FieldName == "CreatedAt");
        Assert.DoesNotContain(history, h => h.FieldName == "CreatedBy");
    }
}
