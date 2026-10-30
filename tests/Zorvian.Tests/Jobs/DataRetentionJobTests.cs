using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Jobs;

namespace Zorvian.Tests.Jobs;

/// <summary>
/// <see cref="DataRetentionJob"/> es la red de seguridad periódica contra el incidente de
/// Neon (proyecto de 1024 MB): si la base está llena, el job NO puede tumbar el proceso
/// de Hangfire — el propio <c>DELETE</c> es lo que libera espacio y el siguiente ciclo
/// reintenta.
/// </summary>
public sealed class DataRetentionJobTests
{
    private readonly Mock<IDatabaseMaintenanceService> _maintenance = new();

    private static DataRetentionPurgeSummary Summary(long rowsDeleted) => new(
        ExecutedAtUtc: DateTime.UtcNow,
        TotalRowsDeleted: rowsDeleted,
        TotalSizeBeforeBytes: 1_000,
        TotalSizeAfterBytes: 900,
        Results: rowsDeleted == 0
            ? Array.Empty<RetentionPurgeResult>()
            : new[]
            {
                new RetentionPurgeResult(
                    TableName: "GpsPositions",
                    Label: "Posiciones GPS",
                    RetentionDays: 90,
                    CutoffUtc: DateTime.UtcNow.AddDays(-90),
                    RowsDeleted: rowsDeleted,
                    SizeBeforeBytes: 1_000,
                    SizeAfterBytes: 900),
            },
        Notes: Array.Empty<string>());

    private static PostgresException StorageFull() => new(
        "could not extend file because project size limit (1024 MB) has been exceeded",
        "ERROR",
        "ERROR",
        PostgresStorageErrors.StorageFullSqlState,
        detail: "This limit is defined externally by the project size limit, and internally by neon.max_cluster_size GUC");

    [Fact]
    public async Task RunAsync_PurgesExpiredData()
    {
        _maintenance
            .Setup(m => m.PurgeExpiredDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Summary(42));

        var job = new DataRetentionJob(_maintenance.Object, NullLogger<DataRetentionJob>.Instance);

        var ex = await Record.ExceptionAsync(() => job.RunAsync());

        Assert.Null(ex);
        _maintenance.Verify(
            m => m.PurgeExpiredDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenThereIsNothingToPurge_DoesNotThrow()
    {
        _maintenance
            .Setup(m => m.PurgeExpiredDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Summary(0));

        var job = new DataRetentionJob(_maintenance.Object, NullLogger<DataRetentionJob>.Instance);

        var ex = await Record.ExceptionAsync(() => job.RunAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task RunAsync_WhenDatabaseIsFull_LogsWarningAndDoesNotThrow()
    {
        // Escenario real: Neon llega a su límite y hasta el DELETE devuelve 53100.
        // El job debe sobrevivir (si relanzara, Hangfire reintentaría en bucle con la
        // base igual de llena y ensuciaría el historial de jobs).
        _maintenance
            .Setup(m => m.PurgeExpiredDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed", StorageFull()));

        var job = new DataRetentionJob(_maintenance.Object, NullLogger<DataRetentionJob>.Instance);

        var ex = await Record.ExceptionAsync(() => job.RunAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task RunAsync_WhenPurgeFails_LogsErrorAndDoesNotThrow()
    {
        _maintenance
            .Setup(m => m.PurgeExpiredDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("timeout"));

        var job = new DataRetentionJob(_maintenance.Object, NullLogger<DataRetentionJob>.Instance);

        var ex = await Record.ExceptionAsync(() => job.RunAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_PropagatesCancellation()
    {
        // La cancelación (deploy/shutdown de Render) NO es un fallo de la purga: debe
        // propagarse para que Hangfire marque el job como cancelado y no como exitoso.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _maintenance
            .Setup(m => m.PurgeExpiredDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var job = new DataRetentionJob(_maintenance.Object, NullLogger<DataRetentionJob>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.RunAsync());
    }
}
