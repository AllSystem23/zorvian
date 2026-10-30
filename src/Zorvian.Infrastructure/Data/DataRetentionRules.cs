namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Política de retención de una tabla de alto volumen.
///
/// <para>
/// <see cref="EntityType"/> es el tipo CLR mapeado: solo se usa en el camino sin
/// PostgreSQL (tests con el proveedor InMemory). En PostgreSQL el borrado va por SQL
/// (por <c>ctid</c>, en lotes) para no cargar filas en memoria — Render tiene 512 MB y
/// la versión anterior de <c>AuditLogCleanupJob</c> hacía <c>ToListAsync()</c> de TODO
/// lo vencido.
/// </para>
/// </summary>
public sealed record RetentionRule(
    Type EntityType,
    string Table,
    string DateColumn,
    int RetentionDays,
    string Label)
{
    /// <summary>Fecha de corte para la corrida actual.</summary>
    public DateTime CutoffUtc(DateTime nowUtc) => nowUtc.AddDays(-RetentionDays);
}

/// <summary>
/// Tablas que crecen sin techo y donde perder las filas vencidas es seguro:
/// logs de entrega de webhooks, telemetría GPS, historial de campos, journal de sync,
/// logs de webhooks de PalmTrack y tokens ya vencidos.
///
/// Deliberadamente NO incluye datos de negocio (ventas, asientos, nómina, documentos):
/// para el HTML de <c>DocumentVersions</c> existe <c>DocumentVersionContentSweepJob</c>
/// (lo mueve a Storage sin borrar nada) y las plantillas quedan en la BD porque pesan
/// poco (~1 MB) y son catálogo.
/// </summary>
public static class DataRetentionRules
{
    /// <summary>
    /// Log de auditoría: se conserva 6 meses. Lo purga <c>AuditLogCleanupJob</c> (job
    /// histórico, id <c>audit-log-cleanup</c>), por eso NO está en <see cref="All"/>
    /// — así la retención genérica no lo vuelve a borrar en la misma ventana.
    /// </summary>
    public static readonly RetentionRule AuditLogs = new(
        typeof(Core.Entities.AuditLog), "AuditLogs", "CreatedAt", 180, "Logs de auditoría");

    /// <summary>Reglas que purga <c>DataRetentionJob</c> (<see cref="AuditLogs"/> aparte).</summary>
    public static readonly IReadOnlyList<RetentionRule> All = new List<RetentionRule>
    {
        // Cada entrega de webhook guarda su payload completo; con reintentos son varias
        // filas por evento. Un mes alcanza para diagnosticar integraciones.
        new(typeof(Core.Entities.WebhookDeliveryLog), "WebhookDeliveryLogs", "ExecutedAt", 30,
            "Entregas de webhooks"),

        // Payload de eventos de PalmTrack (idempotencia + auditoría de la integración).
        new(typeof(Core.Entities.PalmTrackWebhookLog), "PalmTrackWebhookLogs", "ReceivedAt", 30,
            "Webhooks de PalmTrack"),

        // Telemetría: un ping por vehículo cada pocos segundos es la tabla que más crece
        // en flotas conectadas. 90 días cubren reportes trimestrales de rutas.
        new(typeof(Core.Entities.Fleet.GpsPosition), "GpsPositions", "GpsTimestamp", 90,
            "Posiciones GPS"),

        // Historial campo a campo de cada edición (OldValue/NewValue).
        new(typeof(Core.Entities.EntityHistory), "EntityHistories", "CreatedAt", 180,
            "Historial de cambios de entidades"),

        // Journal de sincronización offline→servidor.
        new(typeof(Core.Entities.SyncJournal), "SyncJournals", "OccurredAt", 90,
            "Journal de sincronización"),

        // Tokens vencidos/revocados: la columna es ExpiresAt, así que "ExpiresAt < corte"
        // significa vencidos hace más de N días.
        new(typeof(Core.Entities.RefreshToken), "RefreshTokens", "ExpiresAt", 30,
            "Refresh tokens vencidos"),

        new(typeof(Core.Entities.PasswordResetToken), "PasswordResetTokens", "CreatedAt", 30,
            "Tokens de restablecimiento de contraseña"),
    };

    /// <summary>Regla por tabla (case-insensitive) o <c>null</c>.</summary>
    public static RetentionRule? Find(string tableName) =>
        All.FirstOrDefault(r => string.Equals(r.Table, tableName, StringComparison.OrdinalIgnoreCase));
}
