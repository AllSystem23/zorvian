using Npgsql;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Detección del error de "disco lleno" de PostgreSQL/Neon.
///
/// Neon corta la escritura con <c>53100: could not extend file because project size
/// limit (1024 MB) has been exceeded</c> cuando el proyecto alcanza su límite de
/// almacenamiento. Ese error llega envuelto en <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>
/// (y a veces en un <see cref="AggregateException"/>), así que se busca en toda la
/// cadena de excepciones internas.
///
/// Se usa para:
/// <list type="bullet">
///   <item>devolver un 503 legible en vez de un 500 genérico,</item>
///   <item>evitar el crash-loop de arranque (los seeders son idempotentes),</item>
///   <item>y detener las corridas de seeders cuando ya no se puede escribir nada.</item>
/// </list>
/// </summary>
public static class PostgresStorageErrors
{
    /// <summary>SQLSTATE de "disk full" (Neon lo devuelve al exceder el límite del proyecto).</summary>
    public const string StorageFullSqlState = "53100";

    /// <summary>SQLSTATE de "insufficient resources" (disco lleno genérico).</summary>
    public const string InsufficientResourcesSqlState = "53000";

    /// <summary>Límite del plan gratuito de Neon: 1024 MB por proyecto.</summary>
    public const long DefaultNeonLimitBytes = 1024L * 1024 * 1024;

    /// <summary>
    /// Devuelve la <see cref="PostgresException"/> de almacenamiento lleno si aparece en
    /// la cadena, o <c>null</c>. Recorre <see cref="AggregateException.InnerExceptions"/>
    /// e <see cref="Exception.InnerException"/> con un tope de profundidad para no colgarse
    /// con cadenas circulares.
    /// </summary>
    public static PostgresException? FindStorageFull(Exception? exception, int maxDepth = 12)
    {
        if (exception is null || maxDepth <= 0) return null;

        if (exception is PostgresException pg && IsStorageFullSqlState(pg.SqlState))
            return pg;

        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                var found = FindStorageFull(inner, maxDepth - 1);
                if (found is not null) return found;
            }

            return null;
        }

        return FindStorageFull(exception.InnerException, maxDepth - 1);
    }

    /// <summary>¿La excepción (o alguna de sus internas) es "no queda espacio"?</summary>
    public static bool IsStorageFull(Exception? exception) => FindStorageFull(exception) is not null;

    /// <summary>
    /// <c>53100</c> (disk full) o <c>53000</c> (insufficient resources). Ambos dejan la
    /// base de datos en modo solo-lectura efectivo hasta liberar espacio.
    /// </summary>
    public static bool IsStorageFullSqlState(string? sqlState) =>
        sqlState == StorageFullSqlState || sqlState == InsufficientResourcesSqlState;
}
