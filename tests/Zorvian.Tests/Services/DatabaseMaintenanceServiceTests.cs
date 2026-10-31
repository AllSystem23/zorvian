using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

/// <summary>
/// <see cref="DatabaseMaintenanceService"/> nació del incidente de Neon (proyecto de
/// 1024 MB lleno → <c>53100</c> en toda escritura): el reporte debe explicar QUÉ ocupa el
/// espacio y la purga debe borrar SOLO filas vencidas, respetando la retención configurada
/// y sin tocar datos de negocio ni los <c>AuditLogs</c> (que purga <c>AuditLogCleanupJob</c>).
///
/// Los tests usan el proveedor InMemory, así que ejercitan el camino EF de la purga; el
/// camino PostgreSQL (borrado por <c>ctid</c>, <c>pg_total_relation_size</c> y
/// <c>VACUUM</c>) se valida a mano con <c>docs/DB_STORAGE_RECOVERY.md</c>.
/// </summary>
public sealed class DatabaseMaintenanceServiceTests
{
    private readonly Mock<ITenantContext> _tenant = new();

    private ZorvianDbContext CreateDb(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;
        _tenant.Setup(t => t.TenantId).Returns("tenant-1");
        return new ZorvianDbContext(options, _tenant.Object);
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static DatabaseMaintenanceService CreateSut(ZorvianDbContext db, IConfiguration? configuration = null) =>
        new(db, configuration ?? Config(), NullLogger<DatabaseMaintenanceService>.Instance);

    /// <summary>
    /// Siembra una fila vencida y una vigente por cada tabla de <see cref="DataRetentionRules.All"/>,
    /// más un <c>AuditLog</c> viejo que NO debe tocarse.
    /// </summary>
    private static void SeedOneExpiredAndOneFresh(ZorvianDbContext db, DateTime now)
    {
        db.WebhookDeliveryLogs.AddRange(
            new WebhookDeliveryLog { Id = Guid.NewGuid(), ExecutedAt = now.AddDays(-40) },
            new WebhookDeliveryLog { Id = Guid.NewGuid(), ExecutedAt = now.AddDays(-1) });

        db.PalmTrackWebhookLogs.AddRange(
            new PalmTrackWebhookLog { Id = Guid.NewGuid(), ReceivedAt = now.AddDays(-40) },
            new PalmTrackWebhookLog { Id = Guid.NewGuid(), ReceivedAt = now.AddDays(-1) });

        db.GpsPositions.AddRange(
            new GpsPosition { Id = Guid.NewGuid(), GpsTimestamp = now.AddDays(-120) },
            new GpsPosition { Id = Guid.NewGuid(), GpsTimestamp = now.AddDays(-1) });

        db.EntityHistories.AddRange(
            new EntityHistory { Id = Guid.NewGuid(), CreatedAt = now.AddDays(-200) },
            new EntityHistory { Id = Guid.NewGuid(), CreatedAt = now.AddDays(-1) });

        db.SyncJournals.AddRange(
            new SyncJournal { Id = Guid.NewGuid(), OccurredAt = now.AddDays(-120) },
            new SyncJournal { Id = Guid.NewGuid(), OccurredAt = now.AddDays(-1) });

        db.RefreshTokens.AddRange(
            new RefreshToken { Id = Guid.NewGuid(), Token = "old", ExpiresAt = now.AddDays(-40) },
            new RefreshToken { Id = Guid.NewGuid(), Token = "fresh", ExpiresAt = now.AddDays(-1) });

        db.PasswordResetTokens.AddRange(
            new PasswordResetToken { Id = Guid.NewGuid(), TokenHash = "hash-old", CreatedAt = now.AddDays(-40) },
            new PasswordResetToken { Id = Guid.NewGuid(), TokenHash = "hash-fresh", CreatedAt = now.AddDays(-1) });

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "Sale",
            EntityId = Guid.NewGuid().ToString(),
            Action = "Create",
            CreatedAt = now.AddDays(-400),
        });

        db.SaveChanges();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Reporte de almacenamiento
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStorageReportAsync_WithoutPostgres_ReturnsUnavailableReport_InsteadOfThrowing()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var sut = CreateSut(db);

        // InMemory no tiene DbConnection: antes esto lanzaba (GetDbConnection es
        // relacional) y el endpoint de diagnóstico devolvía 500 justo cuando se necesitaba.
        var report = await sut.GetStorageReportAsync();

        Assert.Contains("InMemory", report.Provider);
        Assert.Null(report.LimitBytes);
        Assert.Null(report.UsagePercent);
        Assert.Empty(report.LargestTables);
        Assert.False(report.IsStorageFull);
        Assert.Single(report.Recommendations);
        Assert.Contains("PostgreSQL", report.Recommendations[0]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Purga de retención
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PurgeExpiredDataAsync_DeletesOnlyRowsPastRetention_ForEveryTable()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        SeedOneExpiredAndOneFresh(db, DateTime.UtcNow);

        var summary = await CreateSut(db).PurgeExpiredDataAsync();

        // Una fila vencida por tabla (7 reglas) + el AuditLog de 400 días (la purga del
        // API incluye la tabla de auditoría, además del job `audit-log-cleanup`).
        Assert.Equal(DataRetentionRules.All.Count + 1, summary.Results.Count);
        Assert.Equal(DataRetentionRules.All.Count + 1, summary.TotalRowsDeleted);
        Assert.All(summary.Results, r => Assert.Equal(1, r.RowsDeleted));

        Assert.Single(db.WebhookDeliveryLogs.IgnoreQueryFilters());
        Assert.Single(db.PalmTrackWebhookLogs.IgnoreQueryFilters());
        Assert.Single(db.GpsPositions.IgnoreQueryFilters());
        Assert.Single(db.EntityHistories.IgnoreQueryFilters());
        Assert.Single(db.SyncJournals.IgnoreQueryFilters());
        Assert.Single(db.RefreshTokens.IgnoreQueryFilters());
        Assert.Single(db.PasswordResetTokens);
        Assert.Empty(db.AuditLogs.IgnoreQueryFilters());

        // La fila que queda es la vigente, no la vencida.
        Assert.True(db.WebhookDeliveryLogs.IgnoreQueryFilters().Single().ExecutedAt > DateTime.UtcNow.AddDays(-2));
        Assert.True(db.GpsPositions.IgnoreQueryFilters().Single().GpsTimestamp > DateTime.UtcNow.AddDays(-2));
        Assert.Equal("fresh", db.RefreshTokens.IgnoreQueryFilters().Single().Token);

        Assert.Contains(summary.Notes, n => n.Contains("vía EF"));
    }

    [Fact]
    public async Task PurgeExpiredDataAsync_PurgesExpiredAuditLogs()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        SeedOneExpiredAndOneFresh(db, DateTime.UtcNow);

        await CreateSut(db).PurgeExpiredDataAsync();

        // AuditLogs NO está en DataRetentionRules.All (lo purga también su propio job),
        // pero la purga del API lo incluye: si no, el operador lanzaba la purga para
        // liberar espacio y la tabla más grande de logs seguía intacta. El borrado solo
        // es posible dentro del scope de retención (AUD-004).
        Assert.DoesNotContain(DataRetentionRules.All, r => r.Table == "AuditLogs");
        Assert.Empty(db.AuditLogs.IgnoreQueryFilters());
        Assert.False(AuditRetentionScope.IsActive);
    }

    [Fact]
    public async Task PurgeExpiredDataAsync_RespectsConfiguredRetention()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var now = DateTime.UtcNow;

        db.GpsPositions.AddRange(
            new GpsPosition { Id = Guid.NewGuid(), GpsTimestamp = now.AddDays(-5) },
            new GpsPosition { Id = Guid.NewGuid(), GpsTimestamp = now.AddHours(-2) });
        db.SaveChanges();

        // Override por configuración (equivale a Database__Retention__GpsPositions=1).
        var sut = CreateSut(db, Config(("Database:Retention:GpsPositions", "1")));

        var summary = await sut.PurgeExpiredDataAsync();

        var gps = summary.Results.Single(r => r.TableName == "GpsPositions");
        Assert.Equal(1, gps.RetentionDays);
        Assert.Equal(1, gps.RowsDeleted);
        Assert.Equal(1, await db.GpsPositions.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task PurgeExpiredDataAsync_DeletesInBatches_WhenBatchSizeIsSmall()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var now = DateTime.UtcNow;

        for (var i = 0; i < 5; i++)
            db.GpsPositions.Add(new GpsPosition { Id = Guid.NewGuid(), GpsTimestamp = now.AddDays(-200) });
        db.SaveChanges();

        var summary = await CreateSut(db).PurgeExpiredDataAsync(batchSize: 2);

        var gps = summary.Results.Single(r => r.TableName == "GpsPositions");
        Assert.Equal(5, gps.RowsDeleted);
        Assert.Equal(0, await db.GpsPositions.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task PurgeExpiredDataAsync_WhenNothingIsExpired_DeletesNothing()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var now = DateTime.UtcNow;

        db.GpsPositions.Add(new GpsPosition { Id = Guid.NewGuid(), GpsTimestamp = now.AddDays(-1) });
        db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog { Id = Guid.NewGuid(), ExecutedAt = now.AddDays(-1) });
        db.SaveChanges();

        var summary = await CreateSut(db).PurgeExpiredDataAsync();

        Assert.Equal(0, summary.TotalRowsDeleted);
        Assert.All(summary.Results, r => Assert.Equal(0, r.RowsDeleted));
        Assert.Equal(1, await db.GpsPositions.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public void ResolveRetentionDays_UsesRuleDefault_UnlessOverridden()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var gps = DataRetentionRules.Find("gpspositions")!;

        Assert.Equal(90, CreateSut(db).ResolveRetentionDays(gps));

        var configured = CreateSut(db, Config(("Database:Retention:GpsPositions", "7")));
        Assert.Equal(7, configured.ResolveRetentionDays(gps));
    }

    [Fact]
    public void DataRetentionRules_NeverIncludeBusinessTables()
    {
        // Guarda contra un futuro "purga todo lo viejo": ninguna tabla de negocio ni de
        // documentos puede entrar en la retención automática.
        var forbidden = new[]
        {
            "Sales", "Purchases", "PayrollDetails", "AccountingEntries", "Documents",
            "GeneratedDocuments", "DocumentVersions", "DocumentTemplates", "Clients",
            "Products", "Employees", "Credits", "Invoices", "AuditLogs",
        };

        foreach (var table in forbidden)
            Assert.Null(DataRetentionRules.Find(table));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Purga de emergencia (TRUNCATE)
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TruncateRetentionTablesAsync_EmptiesOnlyRequestedTables()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var now = DateTime.UtcNow;

        db.GpsPositions.AddRange(
            new GpsPosition { Id = Guid.NewGuid(), GpsTimestamp = now },
            new GpsPosition { Id = Guid.NewGuid(), GpsTimestamp = now.AddDays(-1) });
        db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog { Id = Guid.NewGuid(), ExecutedAt = now });
        db.SaveChanges();

        var summary = await CreateSut(db).TruncateRetentionTablesAsync(new[] { "GpsPositions" });

        var result = Assert.Single(summary.Results);
        Assert.Equal("GpsPositions", result.TableName);
        Assert.Equal(0, await db.GpsPositions.IgnoreQueryFilters().CountAsync());
        // Un TRUNCATE de una tabla no toca las demás.
        Assert.Equal(1, await db.WebhookDeliveryLogs.IgnoreQueryFilters().CountAsync());
        Assert.Contains(summary.Notes, n => n.Contains("vía EF"));
    }

    [Fact]
    public async Task TruncateRetentionTablesAsync_IgnoresBusinessTables()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        db.Departments.Add(new Department
        {
            Id = Guid.NewGuid(),
            Name = "Ventas",
            Code = "VTA",
            TenantId = "tenant-1",
        });
        db.SaveChanges();

        var summary = await CreateSut(db)
            .TruncateRetentionTablesAsync(new[] { "Departments", "Sales" });

        Assert.Empty(summary.Results);
        Assert.Contains(summary.Notes, n => n.Contains("Departments"));
        Assert.Contains(summary.Notes, n => n.Contains("datos de negocio"));
        Assert.Equal(1, await db.Departments.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task TruncateRetentionTablesAsync_AuditLogsRequiresExplicitOptIn()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "Sale",
            EntityId = "1",
            Action = "Create",
            CreatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();

        // Sin includeAuditLogs no se vacía (se conserva 6 meses por AUD-004).
        var withoutOptIn = await CreateSut(db).TruncateRetentionTablesAsync(new[] { "AuditLogs" });

        Assert.Empty(withoutOptIn.Results);
        Assert.Single(db.AuditLogs.IgnoreQueryFilters());
        Assert.Contains(withoutOptIn.Notes, n => n.Contains("includeAuditLogs=true"));

        var withOptIn = await CreateSut(db)
            .TruncateRetentionTablesAsync(new[] { "AuditLogs" }, includeAuditLogs: true);

        Assert.Single(withOptIn.Results);
        Assert.Empty(db.AuditLogs.IgnoreQueryFilters());
        // El scope de retención se cierra al salir: el resto del proceso sigue bloqueado.
        Assert.False(AuditRetentionScope.IsActive);
    }

    [Fact]
    public async Task TruncateRetentionTablesAsync_WithoutTables_EmptiesEveryRetentionTable()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        SeedOneExpiredAndOneFresh(db, DateTime.UtcNow);

        var summary = await CreateSut(db).TruncateRetentionTablesAsync();

        Assert.Equal(DataRetentionRules.All.Count, summary.Results.Count);
        Assert.Equal(0, await db.GpsPositions.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0, await db.WebhookDeliveryLogs.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0, await db.RefreshTokens.IgnoreQueryFilters().CountAsync());
        // AuditLogs sigue fuera del barrido por defecto.
        Assert.Single(db.AuditLogs.IgnoreQueryFilters());
    }

    [Fact]
    public void ResolveTruncateTargets_OnlyAllowsRetentionRules()
    {
        var (targets, unknown) = DatabaseMaintenanceService.ResolveTruncateTargets(
            new[] { "gpspositions", "Sales", "AuditLogs", " " }, includeAuditLogs: false);

        Assert.Equal(new[] { "GpsPositions" }, targets.Select(t => t.Table));
        Assert.Contains("Sales", unknown);
        Assert.Contains("AuditLogs", unknown);

        var (withAudit, unknownWithAudit) = DatabaseMaintenanceService.ResolveTruncateTargets(
            new[] { "AuditLogs" }, includeAuditLogs: true);

        Assert.Equal("AuditLogs", Assert.Single(withAudit).Table);
        Assert.Empty(unknownWithAudit);
    }
}
