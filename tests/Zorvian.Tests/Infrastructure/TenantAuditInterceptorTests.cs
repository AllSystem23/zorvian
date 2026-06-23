using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Infrastructure;

public sealed class TenantAuditInterceptorTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<ILogger<TenantAuditInterceptor>> _logger = new();
    private readonly TenantAuditInterceptor _interceptor;
    private readonly string _tenantId;
    private readonly Guid _userId;

    public TenantAuditInterceptorTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        _userId = Guid.NewGuid();
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _tenant.Setup(t => t.CurrentUserId).Returns(_userId);
        _interceptor = new TenantAuditInterceptor(_tenant.Object, _logger.Object);
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
    public async Task AddedEntity_GetsTenantIdFromContext()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Test Dept",
            Code = "TST",
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        Assert.Equal(_tenantId, dept.TenantId);
    }

    [Fact]
    public async Task AddedEntity_GetsCreatedByFromCurrentUserId()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Test Dept",
            Code = "TST",
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        Assert.Equal(_userId.ToString(), dept.CreatedBy);
    }

    [Fact]
    public async Task AddedEntity_GetsCreatedAtSetToUtcNow()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var before = DateTime.UtcNow.AddSeconds(-1);

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Test Dept",
            Code = "TST",
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        Assert.True(dept.CreatedAt >= before);
        Assert.True(dept.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AddedEntity_DoesNotOverwriteExistingTenantId()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var existingTenantId = "existing-tenant-id";

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Test Dept",
            Code = "TST",
            TenantId = existingTenantId,
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        Assert.Equal(existingTenantId, dept.TenantId);
    }

    [Fact]
    public async Task AddedEntity_DoesNotOverwriteExistingCreatedBy()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var existingCreatedBy = "manual-service";

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Test Dept",
            Code = "TST",
            CreatedBy = existingCreatedBy,
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        Assert.Equal(existingCreatedBy, dept.CreatedBy);
    }

    [Fact]
    public async Task ModifiedEntity_GetsUpdatedAtSetToUtcNow()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Before",
            Code = "BEF",
            TenantId = _tenantId,
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        Assert.Null(dept.UpdatedAt);

        var before = DateTime.UtcNow.AddSeconds(-1);
        dept.Name = "After";
        await db.SaveChangesAsync();

        Assert.NotNull(dept.UpdatedAt);
        Assert.True(dept.UpdatedAt >= before);
    }

    [Fact]
    public async Task ModifiedEntity_GetsUpdatedByFromCurrentUserId()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Before",
            Code = "BEF",
            TenantId = _tenantId,
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        dept.Name = "After";
        await db.SaveChangesAsync();

        Assert.Equal(_userId.ToString(), dept.UpdatedBy);
    }

    [Fact]
    public async Task AddedEntity_EmptyTenantId_LogsWarning()
    {
        _tenant.Setup(t => t.TenantId).Returns(string.Empty);
        var interceptor = new TenantAuditInterceptor(_tenant.Object, _logger.Object);

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var db = new ZorvianDbContext(options, _tenant.Object);

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "No Tenant",
            Code = "NT",
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TenantId is empty")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MultipleEntities_AllGetAuditFields()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.Departments.Add(new Department
        {
            Id = Guid.NewGuid(),
            Name = "Dept 1",
            Code = "D1",
        });

        db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Juan",
            LastName = "Perez",
            Email = "juan@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
        });

        await db.SaveChangesAsync();

        var dept = await db.Departments.FirstAsync(d => d.Code == "D1");
        var emp = await db.Employees.FirstAsync(e => e.FirstName == "Juan");

        Assert.Equal(_tenantId, dept.TenantId);
        Assert.Equal(_tenantId, emp.TenantId);
        Assert.Equal(_userId.ToString(), dept.CreatedBy);
        Assert.Equal(_userId.ToString(), emp.CreatedBy);
    }

    // ──────────────────────────────────────────────
    // CompanyId assignment tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task AddedEntity_GetsCompanyIdFromTenantId()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Test Dept",
            Code = "TST",
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        var expectedCompanyId = Guid.Parse(_tenantId);
        Assert.Equal(expectedCompanyId, dept.CompanyId);
    }

    [Fact]
    public async Task AddedEntity_DoesNotOverwriteExistingCompanyId()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var existingCompanyId = Guid.NewGuid();

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Test Dept",
            Code = "TST",
            CompanyId = existingCompanyId,
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        Assert.Equal(existingCompanyId, dept.CompanyId);
    }

    [Fact]
    public async Task AddedEntity_CompanyIdStaysEmpty_WhenTenantIdIsNotGuid()
    {
        var nonGuidTenantId = "not-a-guid";
        _tenant.Setup(t => t.TenantId).Returns(nonGuidTenantId);
        var interceptor = new TenantAuditInterceptor(_tenant.Object, _logger.Object);

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var db = new ZorvianDbContext(options, _tenant.Object);

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "No Company",
            Code = "NC",
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        Assert.Equal(Guid.Empty, dept.CompanyId);
    }

    [Fact]
    public async Task AddedEntity_CompanyIdStaysEmpty_WhenTenantIdIsEmpty()
    {
        _tenant.Setup(t => t.TenantId).Returns(string.Empty);
        var interceptor = new TenantAuditInterceptor(_tenant.Object, _logger.Object);

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var db = new ZorvianDbContext(options, _tenant.Object);

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Empty Tenant",
            Code = "ET",
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        Assert.Equal(Guid.Empty, dept.CompanyId);
    }

    [Fact]
    public async Task AddedEntity_LogsWarning_WhenTenantIdNotParseableToCompanyId()
    {
        var nonGuidTenantId = "abc-123";
        _tenant.Setup(t => t.TenantId).Returns(nonGuidTenantId);
        var interceptor = new TenantAuditInterceptor(_tenant.Object, _logger.Object);

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var db = new ZorvianDbContext(options, _tenant.Object);

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Bad Tenant",
            Code = "BT",
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Could not parse TenantId")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ModifiedEntity_DoesNotTouchCompanyId()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Before",
            Code = "BEF",
            TenantId = _tenantId,
        };

        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        var companyIdAfterAdd = dept.CompanyId;

        dept.Name = "After";
        await db.SaveChangesAsync();

        Assert.Equal(companyIdAfterAdd, dept.CompanyId);
    }

    [Fact]
    public async Task MultipleEntities_AllGetCompanyId()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());

        db.Departments.Add(new Department
        {
            Id = Guid.NewGuid(),
            Name = "Dept 1",
            Code = "D1",
        });

        db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Juan",
            LastName = "Perez",
            Email = "juan@test.com",
            Status = "active",
            HireDate = new DateOnly(2025, 1, 1),
        });

        await db.SaveChangesAsync();

        var expectedCompanyId = Guid.Parse(_tenantId);

        var dept = await db.Departments.FirstAsync(d => d.Code == "D1");
        var emp = await db.Employees.FirstAsync(e => e.FirstName == "Juan");

        Assert.Equal(expectedCompanyId, dept.CompanyId);
        Assert.Equal(expectedCompanyId, emp.CompanyId);
    }
}
