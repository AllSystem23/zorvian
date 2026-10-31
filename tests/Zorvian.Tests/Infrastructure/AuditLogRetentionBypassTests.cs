using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Migrations;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Infrastructure;

/// <summary>
/// AUD-004 está implementado DOS veces: en EF (<c>AuditImmutabilityInterceptor</c>) y en
/// PostgreSQL (<c>trg_audit_logs_immutable</c> → <c>audit_logs_block_mutation()</c>).
/// Abrir solo el interceptor no basta: con la base real el borrado moría en el trigger con
/// <c>SQLSTATE P0001</c> ("AUD-004: AuditLogs es inmutable. Operación TRUNCATE ..."), que
/// es exactamente el error que reportó el operador al intentar vaciar la tabla.
///
/// <para>
/// Estos tests cubren la capa de base (SQL de la migración + constante del GUC) y la
/// integración con el scope de EF. El trigger real no se puede ejecutar aquí (los tests
/// usan InMemory); la validación en PostgreSQL vivo está en
/// <c>docs/DB_STORAGE_RECOVERY.md</c>.
/// </para>
/// </summary>
public sealed class AuditLogRetentionBypassTests
{
    private readonly Mock<ITenantContext> _tenant = new();

    private static AddAuditLogRetentionBypass NewMigration() => new();

    private static string SqlOf(Microsoft.EntityFrameworkCore.Migrations.Migration migration, bool up)
    {
        // La migración es SQL puro (sin Designer): UpOperations/DownOperations ejecuta
        // Up()/Down() sobre un builder en memoria y devuelve las operaciones.
        migration.ActiveProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";

        var operations = up ? migration.UpOperations : migration.DownOperations;
        return string.Join("\n", operations.OfType<SqlOperation>().Select(o => o.Sql));
    }

    private ZorvianDbContext CreateDbWithImmutabilityGuard(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .AddInterceptors(new AuditImmutabilityInterceptor())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns("tenant-1");
        return new ZorvianDbContext(options, _tenant.Object);
    }

    private static DatabaseMaintenanceService CreateSut(ZorvianDbContext db) =>
        new(db, new ConfigurationBuilder().Build(), NullLogger<DatabaseMaintenanceService>.Instance);

    // ─────────────────────────────────────────────────────────────────────────────
    // Descubribilidad y contrato de la migración
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Migration_HasAttribute_WithExpectedId()
    {
        var attr = typeof(AddAuditLogRetentionBypass).GetCustomAttribute<MigrationAttribute>();

        Assert.NotNull(attr);
        Assert.Equal("20261031120000_AddAuditLogRetentionBypass", attr!.Id);
    }

    [Fact]
    public void Up_AllowsDeleteAndTruncateOnlyThroughTheSessionSetting()
    {
        var sql = SqlOf(NewMigration(), up: true);

        // La función se rehace y consulta el GUC que abre la retención.
        Assert.Contains("CREATE OR REPLACE FUNCTION audit_logs_block_mutation()", sql);
        Assert.Contains($"current_setting('{AuditRetentionScope.DatabaseSetting}', true)", sql);
        Assert.Contains("IF TG_OP IN ('DELETE', 'TRUNCATE')", sql);

        // UPDATE queda FUERA del bypass: el contenido de un log no se altera nunca,
        // ni siquiera durante una purga.
        Assert.DoesNotContain("'UPDATE'", sql);
        Assert.DoesNotContain("UPDATE, 'TRUNCATE'", sql);

        // Sin el GUC sigue bloqueando con el mismo código de error que ve el operador.
        Assert.Contains("RAISE EXCEPTION", sql);
        Assert.Contains("USING ERRCODE = 'P0001'", sql);
        Assert.Contains("AUD-004: AuditLogs es inmutable", sql);
    }

    [Fact]
    public void Up_RecreatesTheStatementTrigger_ForUpdateDeleteAndTruncate()
    {
        var sql = SqlOf(NewMigration(), up: true);

        Assert.Contains("DROP TRIGGER IF EXISTS trg_audit_logs_immutable ON \"AuditLogs\"", sql);
        Assert.Contains("CREATE TRIGGER trg_audit_logs_immutable", sql);
        Assert.Contains("BEFORE UPDATE OR DELETE OR TRUNCATE", sql);
        Assert.Contains("FOR EACH STATEMENT", sql);
        Assert.Contains("EXECUTE FUNCTION audit_logs_block_mutation()", sql);
    }

    [Fact]
    public void Up_RevokesDmlFromAppRole_WhenTheRoleExists()
    {
        var sql = SqlOf(NewMigration(), up: true);

        // Idempotente y guardado: en proyectos donde `zorvian_app` no existe el bloque
        // solo emite un NOTICE (el trigger es el guardia real).
        Assert.Contains("IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'zorvian_app')", sql);
        Assert.Contains("REVOKE UPDATE, DELETE, TRUNCATE ON \"AuditLogs\" FROM zorvian_app", sql);
    }

    [Fact]
    public void Up_DollarQuotesAreBalanced()
    {
        // Regresión del bug que rompió EnableRLS en producción: dentro de un bloque DO no
        // se puede reutilizar el mismo delimitador para los strings internos, porque el
        // primer cierre interno termina el bloque y el resto queda como SQL suelto.
        var sql = SqlOf(NewMigration(), up: true);

        Assert.Equal(2, sql.Split("$fn$").Length - 1);
        Assert.Equal(2, sql.Split("$do$").Length - 1);
        Assert.DoesNotContain("$$", sql);
    }

    [Fact]
    public void Down_RestoresTheStrictFunction_WithoutBypass()
    {
        var sql = SqlOf(NewMigration(), up: false);

        Assert.Contains("CREATE OR REPLACE FUNCTION audit_logs_block_mutation()", sql);
        Assert.Contains("USING ERRCODE = 'P0001'", sql);
        Assert.DoesNotContain(AuditRetentionScope.DatabaseSetting, sql);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Constante del GUC (la usa la purga del API y el job)
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EnableDatabaseSettingSql_IsTransactionLocal()
    {
        // `true` = is_local: la GUC se revierte al terminar la transacción y no queda
        // pegada en la conexión del pool (el contexto y la conexión se reutilizan).
        Assert.Equal(
            "SELECT set_config('zorvian.audit_retention', 'on', true)",
            AuditRetentionScope.EnableDatabaseSettingSql);

        Assert.EndsWith(", true)", AuditRetentionScope.EnableDatabaseSettingSql);
        Assert.Contains("set_config", AuditRetentionScope.EnableDatabaseSettingSql);
    }

    [Fact]
    public void DatabaseSetting_MatchesTheNameConsultedByTheTrigger()
    {
        Assert.Equal("zorvian.audit_retention", AuditRetentionScope.DatabaseSetting);
        Assert.Contains(AuditRetentionScope.DatabaseSetting, SqlOf(NewMigration(), up: true));
        Assert.Contains($"= '{AuditRetentionScope.DatabaseSettingValue}'", SqlOf(NewMigration(), up: true));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Scope de EF + purga (camino no-PostgreSQL: InMemory)
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PurgeExpiredDataAsync_WithImmutabilityInterceptor_DeletesExpiredAuditLogs()
    {
        using var db = CreateDbWithImmutabilityGuard(Guid.NewGuid().ToString());

        db.AuditLogs.AddRange(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "Old",
                EntityId = "1",
                Action = "Create",
                CreatedAt = DateTime.UtcNow.AddDays(-400),
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "Fresh",
                EntityId = "2",
                Action = "Create",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            });
        await db.SaveChangesAsync();

        var summary = await CreateSut(db).PurgeExpiredDataAsync();

        var auditResult = summary.Results.Single(r => r.TableName == "AuditLogs");
        Assert.Equal(1, auditResult.RowsDeleted);
        Assert.Equal("Fresh", Assert.Single(db.AuditLogs.IgnoreQueryFilters()).EntityName);
        Assert.False(AuditRetentionScope.IsActive);
    }

    [Fact]
    public async Task TruncateRetentionTablesAsync_WithImmutabilityInterceptor_EmptiesAuditLogs()
    {
        // Es el escenario exacto del incidente: la purga de emergencia con
        // includeAuditLogs=true. Con el interceptor sin el scope, el SaveChanges lanzaba
        // "AUD-004".
        using var db = CreateDbWithImmutabilityGuard(Guid.NewGuid().ToString());

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "Anything",
            EntityId = "1",
            Action = "Create",
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var summary = await CreateSut(db).TruncateRetentionTablesAsync(
            new[] { "AuditLogs" }, includeAuditLogs: true);

        Assert.Contains(summary.Results, r => r.TableName == "AuditLogs");
        Assert.Empty(db.AuditLogs.IgnoreQueryFilters());
        Assert.False(AuditRetentionScope.IsActive);
    }
}
