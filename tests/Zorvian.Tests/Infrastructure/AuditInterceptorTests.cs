using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Infrastructure;

public sealed class AuditInterceptorTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly AuditInterceptor _interceptor;

    public AuditInterceptorTests()
    {
        _tenant.Setup(t => t.TenantId).Returns("tenant-1");
        _tenant.Setup(t => t.CurrentUserId).Returns((Guid?)null);
        _interceptor = new AuditInterceptor(_tenant.Object);
    }

    private ZorvianDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(_interceptor)
            .Options;

        return new ZorvianDbContext(options, _tenant.Object);
    }

    [Fact]
    public async Task SaveChanges_AddedEntity_CreatesAuditLog()
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

        var auditLogs = await db.AuditLogs.IgnoreQueryFilters().ToListAsync();
        var log = Assert.Single(auditLogs);
        Assert.Equal("Create", log.Action);
        Assert.Equal("Employee", log.EntityName);
        Assert.NotNull(log.NewValues);
    }

    [Fact]
    public async Task SaveChanges_UpdatedEntity_CreatesAuditLog()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var empId = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = empId,
            FirstName = "Before",
            LastName = "Perez",
            Email = "before@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var emp = await db.Employees.FindAsync(empId);
        emp!.FirstName = "After";
        await db.SaveChangesAsync();

        var updateLogs = await db.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.Action == "Update")
            .ToListAsync();

        var log = Assert.Single(updateLogs);
        Assert.Contains("FirstName", log.ChangedProperties);
        Assert.NotNull(log.OldValues);
        Assert.NotNull(log.NewValues);
    }

    [Fact]
    public async Task SaveChanges_DeletedEntity_SoftDeletes()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var empId = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = empId,
            FirstName = "Delete",
            LastName = "Me",
            Email = "del@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var emp = await db.Employees.FindAsync(empId);
        db.Employees.Remove(emp!);
        await db.SaveChangesAsync();

        var softDeleted = await db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == empId);
        Assert.NotNull(softDeleted);
        Assert.True(softDeleted.IsDeleted);
        Assert.NotNull(softDeleted.DeletedAt);
    }

    [Fact]
    public async Task SaveChanges_AuditLogEntity_DoesNotCreateNestedAudit()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.AuditLogs.Add(new AuditLog
        {
            TenantId = "tenant-1",
            EntityName = "Test",
            EntityId = "1",
            Action = "Manual",
        });
        await db.SaveChangesAsync();

        var logs = await db.AuditLogs.IgnoreQueryFilters().ToListAsync();
        Assert.Single(logs);
        Assert.Equal("Manual", logs[0].Action);
    }

    [Fact]
    public async Task SaveChanges_ModifiedEntity_AutoPopulatesUpdatedAt()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Before",
            Code = "BEF",
            TenantId = "tenant-1",
        };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        dept.Name = "After";
        await db.SaveChangesAsync();

        Assert.NotNull(dept.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_EntityWithNullTenantId_GetsTenantIdFromContext()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var emp = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "NoTenant",
            LastName = "Test",
            Email = "nt@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
        };

        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        Assert.Equal("tenant-1", emp.TenantId);
    }

    [Fact]
    public async Task SaveChanges_MultipleOperations_CreatesMultipleAuditLogs()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Multi1",
            LastName = "Test",
            Email = "m1@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
            TenantId = "tenant-1",
        });
        db.Departments.Add(new Department
        {
            Id = Guid.NewGuid(),
            Name = "Multi Dept",
            Code = "MUL",
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var logs = await db.AuditLogs.IgnoreQueryFilters().ToListAsync();
        Assert.Equal(2, logs.Count);
    }

    [Fact]
    public async Task SaveChanges_AddedWithCurrentUser_SetsCreatedBy()
    {
        var tenantWithUser = new Mock<ITenantContext>();
        tenantWithUser.Setup(t => t.TenantId).Returns("tenant-1");
        tenantWithUser.Setup(t => t.CurrentUserId).Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var interceptorWithUser = new AuditInterceptor(tenantWithUser.Object);

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptorWithUser)
            .Options;

        using var db = new ZorvianDbContext(options, tenantWithUser.Object);
        db.Departments.Add(new Department
        {
            Id = Guid.NewGuid(),
            Name = "User Dept",
            Code = "USR",
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var logs = await db.AuditLogs.IgnoreQueryFilters().ToListAsync();
        var log = Assert.Single(logs);
        Assert.Equal("11111111-1111-1111-1111-111111111111", log.PerformedBy.ToString());
    }

    [Fact]
    public async Task SaveChanges_UpdatedWithCurrentUser_SetsUpdatedBy()
    {
        var tenantWithUser = new Mock<ITenantContext>();
        tenantWithUser.Setup(t => t.TenantId).Returns("tenant-1");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        tenantWithUser.Setup(t => t.CurrentUserId).Returns(userId);
        var interceptorWithUser = new AuditInterceptor(tenantWithUser.Object);

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptorWithUser)
            .Options;

        using var db = new ZorvianDbContext(options, tenantWithUser.Object);
        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Before",
            Code = "BEF",
            TenantId = "tenant-1",
        };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        tenantWithUser.Setup(t => t.CurrentUserId).Returns(userId);
        dept.Name = "After";
        await db.SaveChangesAsync();

        Assert.Equal(userId.ToString(), dept.UpdatedBy);
    }
}
