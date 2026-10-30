using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Infrastructure;

/// <summary>
/// Regresión del incidente de Neon: el proyecto llegó a 1024 MB y toda escritura empezó a
/// fallar con <c>53100</c>. La detección tiene que atravesar el envoltorio de EF
/// (<c>DbUpdateException</c>) y los <c>AggregateException</c> de Npgsql.
/// </summary>
public sealed class PostgresStorageErrorsTests
{
    private static PostgresException StorageFull() => new(
        "could not extend file because project size limit (1024 MB) has been exceeded",
        "ERROR",
        "ERROR",
        PostgresStorageErrors.StorageFullSqlState,
        detail: "This limit is defined externally by the project size limit, and internally by neon.max_cluster_size GUC");

    [Fact]
    public void DirectPostgresException_IsDetected()
    {
        Assert.True(PostgresStorageErrors.IsStorageFull(StorageFull()));
    }

    [Fact]
    public void WrappedInDbUpdateException_IsDetected()
    {
        var wrapped = new DbUpdateException(
            "An error occurred while saving the entity changes. See the inner exception for details.",
            StorageFull());

        Assert.True(PostgresStorageErrors.IsStorageFull(wrapped));
        Assert.Equal("53100", PostgresStorageErrors.FindStorageFull(wrapped)!.SqlState);
    }

    [Fact]
    public void WrappedInAggregateException_IsDetected()
    {
        var aggregate = new AggregateException(
            new InvalidOperationException("outer"),
            new DbUpdateException("save failed", StorageFull()));

        Assert.True(PostgresStorageErrors.IsStorageFull(aggregate));
    }

    [Fact]
    public void InsufficientResourcesState_IsTreatedAsStorageFull()
    {
        var ex = new PostgresException("insufficient resources", "ERROR", "ERROR", "53000");

        Assert.True(PostgresStorageErrors.IsStorageFull(ex));
    }

    [Fact]
    public void OtherPostgresError_IsNotStorageFull()
    {
        // unique_violation: un error de datos normal, no de disco.
        var ex = new PostgresException(
            "duplicate key value violates unique constraint", "ERROR", "ERROR", "23505");

        Assert.False(PostgresStorageErrors.IsStorageFull(ex));
        Assert.Null(PostgresStorageErrors.FindStorageFull(ex));
    }

    [Fact]
    public void UnrelatedException_IsNotStorageFull()
    {
        Assert.False(PostgresStorageErrors.IsStorageFull(new InvalidOperationException("nope")));
        Assert.False(PostgresStorageErrors.IsStorageFull(null));
    }

    [Fact]
    public void CircularIsNotWalkedForever()
    {
        // Una cadena muy anidada no debe colgar la búsqueda.
        Exception current = new InvalidOperationException("deep");
        for (var i = 0; i < 40; i++)
            current = new InvalidOperationException($"level {i}", current);

        Assert.False(PostgresStorageErrors.IsStorageFull(current));
    }
}
