using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Jobs;

namespace Zorvian.Tests.Jobs;

public sealed class AuditLogCleanupJobTests
{
    private readonly Mock<ILogger<AuditLogCleanupJob>> _logger = new();
    private readonly Mock<ITenantContext> _tenant = new();

    private ZorvianDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        _tenant.Setup(t => t.TenantId).Returns("tenant-1");
        return new ZorvianDbContext(options, _tenant.Object);
    }

    [Fact]
    public async Task RunAsync_DeletesLogsOlderThanSixMonths()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.AuditLogs.Add(new AuditLog
        {
            EntityName = "Old",
            EntityId = "1",
            Action = "Create",
            CreatedAt = DateTime.UtcNow.AddMonths(-7),
            TenantId = "tenant-1",
        });
        db.AuditLogs.Add(new AuditLog
        {
            EntityName = "Recent",
            EntityId = "2",
            Action = "Update",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var job = new AuditLogCleanupJob(db, _logger.Object);
        await job.RunAsync();

        var remaining = await db.AuditLogs.IgnoreQueryFilters().OrderBy(a => a.CreatedAt).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("Recent", remaining[0].EntityName);
        Assert.Equal("Update", remaining[0].Action);
    }

    [Fact]
    public async Task RunAsync_IgnoresLogsNewerThanCutoff()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.AuditLogs.Add(new AuditLog
        {
            EntityName = "FiveMonths",
            EntityId = "1",
            Action = "Create",
            CreatedAt = DateTime.UtcNow.AddMonths(-5),
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var job = new AuditLogCleanupJob(db, _logger.Object);
        await job.RunAsync();

        var remaining = await db.AuditLogs.IgnoreQueryFilters().ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("FiveMonths", remaining[0].EntityName);
    }

    [Fact]
    public async Task RunAsync_WhenNoOldLogs_DoesNothing()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.AuditLogs.Add(new AuditLog
        {
            EntityName = "New",
            EntityId = "1",
            Action = "Create",
            CreatedAt = DateTime.UtcNow,
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var job = new AuditLogCleanupJob(db, _logger.Object);
        await job.RunAsync();

        var remaining = await db.AuditLogs.IgnoreQueryFilters().ToListAsync();
        Assert.Single(remaining);
    }

    [Fact]
    public async Task RunAsync_WhenNoLogsAtAll_DoesNotThrow()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var job = new AuditLogCleanupJob(db, _logger.Object);
        var ex = await Record.ExceptionAsync(() => job.RunAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task RunAsync_OnlyDeletesAuditLogs_NotOtherEntities()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.AuditLogs.Add(new AuditLog
        {
            EntityName = "OldLog",
            EntityId = "1",
            Action = "Create",
            CreatedAt = DateTime.UtcNow.AddMonths(-7),
            TenantId = "tenant-1",
        });
        var empId = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = empId,
            FirstName = "Keep",
            LastName = "Me",
            Email = "keep@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
            CreatedAt = DateTime.UtcNow.AddMonths(-7),
            TenantId = "tenant-1",
        });
        await db.SaveChangesAsync();

        var job = new AuditLogCleanupJob(db, _logger.Object);
        await job.RunAsync();

        var employees = await db.Employees.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(employees);
        Assert.Single(employees);
    }
}
