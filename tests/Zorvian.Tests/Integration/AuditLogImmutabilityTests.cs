using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Zorvian.Web;
using Zorvian.Infrastructure.Data;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Integration;

/// <summary>
/// Tests para AUD-004: Validan que los registros de <see cref="AuditLog"/>
/// son inmutables tanto a nivel de EF Core (interceptor) como a nivel de
/// base de datos (trigger PostgreSQL).
///
/// Estos tests usan el <see cref="CustomWebApplicationFactory"/> que se
/// reutiliza del archivo <c>CompaniesAuthorizationTests.cs</c>. La BD en
/// memoria (InMemory provider) valida la primera capa de defensa (EF Core);
/// la segunda capa (trigger PostgreSQL) se valida en producción cuando
/// se ejecuta la migración <c>AddAuditLogImmutability</c>.
/// </summary>
public class AuditLogImmutabilityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuditLogImmutabilityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InsertAuditLog_ThenModify_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = "test-tenant",
            EntityName = "TestEntity",
            EntityId = Guid.NewGuid().ToString(),
            Action = "Create",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            IsDeleted = false
        };

        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync();

        // Detach para poder modificar manualmente
        dbContext.ChangeTracker.Clear();

        // Act — intentar modificar el registro
        var tracked = dbContext.AuditLogs
            .IgnoreQueryFilters()
            .First(a => a.Id == auditLog.Id);
        tracked.Action = "Modified-Value";
        tracked.UpdatedAt = DateTime.UtcNow;

        // Assert — debe lanzar InvalidOperationException por AUD-004
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dbContext.SaveChangesAsync());

        Assert.Contains("AUD-004", ex.Message);
        Assert.Contains("inmutables", ex.Message);
    }

    [Fact]
    public async Task InsertAuditLog_ThenDelete_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = "test-tenant",
            EntityName = "TestEntity",
            EntityId = Guid.NewGuid().ToString(),
            Action = "Update",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            IsDeleted = false
        };

        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        // Act — intentar eliminar el registro
        var tracked = dbContext.AuditLogs
            .IgnoreQueryFilters()
            .First(a => a.Id == auditLog.Id);
        dbContext.AuditLogs.Remove(tracked);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dbContext.SaveChangesAsync());

        Assert.Contains("AUD-004", ex.Message);
        Assert.Contains("Deleted", ex.Message);
    }

    [Fact]
    public async Task InsertAuditLog_IsAllowed_ButCannotBeSoftDeleted()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = "test-tenant",
            EntityName = "TestEntity",
            EntityId = Guid.NewGuid().ToString(),
            Action = "Delete",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            IsDeleted = false
        };

        // Act — la inserción SÍ debe permitirse
        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        // Assert — el registro se insertó correctamente
        var saved = dbContext.AuditLogs
            .IgnoreQueryFilters()
            .FirstOrDefault(a => a.Id == auditLog.Id);
        Assert.NotNull(saved);
        Assert.Equal("Delete", saved!.Action);

        // Act — intento soft-delete (modificar IsDeleted a true)
        var tracked = dbContext.AuditLogs
            .IgnoreQueryFilters()
            .First(a => a.Id == auditLog.Id);
        tracked.IsDeleted = true;
        tracked.DeletedAt = DateTime.UtcNow;

        // Assert — debe fallar por inmutabilidad
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dbContext.SaveChangesAsync());

        Assert.Contains("AUD-004", ex.Message);
    }

    [Fact]
    public async Task AuditInterceptor_DoesNotBlockNormalEntityInserts()
    {
        // Arrange — verificar que el interceptor no afecta a entidades NO AuditLog
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();

        // Crear una Company (no AuditLog) debe funcionar normalmente
        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Test Co",
            LegalName = "Test Co S.A.",
            Country = "NI",
            Currency = "NIO",
            Timezone = "America/Managua",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            IsDeleted = false
        };

        dbContext.Companies.Add(company);

        // Act + Assert — NO debe lanzar
        await dbContext.SaveChangesAsync();

        var saved = dbContext.Companies
            .IgnoreQueryFilters()
            .FirstOrDefault(c => c.Id == company.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task AuditInterceptor_GeneratesAuditLogsAutomatically_OnCompanyInsert()
    {
        // Arrange — verificar que el AuditInterceptor existente sigue creando logs
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();

        var initialCount = dbContext.AuditLogs.IgnoreQueryFilters().Count();

        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Audit Co",
            LegalName = "Audit Co S.A.",
            Country = "NI",
            Currency = "NIO",
            Timezone = "America/Managua",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            IsDeleted = false
        };

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();

        // Assert — debe haberse creado un nuevo AuditLog por el AuditInterceptor
        var finalCount = dbContext.AuditLogs.IgnoreQueryFilters().Count();
        Assert.True(finalCount > initialCount, "El AuditInterceptor debió crear al menos un nuevo AuditLog");

        var newLog = dbContext.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.EntityName == nameof(Company) && a.EntityId == company.Id.ToString())
            .FirstOrDefault();
        Assert.NotNull(newLog);
        Assert.Equal("Create", newLog!.Action);
    }
}
