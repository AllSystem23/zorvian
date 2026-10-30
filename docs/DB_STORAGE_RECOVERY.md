# Recuperar espacio en la base de datos (Neon 1024 MB)

Runbook para el error:

```
Npgsql.PostgresException (0x80004005): 53100: could not extend file because
project size limit (1024 MB) has been exceeded
  ... at Npgsql.NpgsqlDataReader.NextResult(...)
  ... at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(...)
```

**Qué significa.** Neon limita el proyecto (plan gratuito: **1024 MB**) y cuenta el tamaño
**lógico** de las relaciones (tablas + índices + TOAST), no las filas vivas. Al llegar al
tope, PostgreSQL rechaza toda escritura: no se puede guardar una venta, una nómina, un
documento, ni siquiera un log de auditoría. Las lecturas siguen funcionando.

**Qué NO es.** No es un bug de una entidad ni de una migración: es falta de disco. El
mensaje de EF (`Failed executing DbCommand` + cientos de parámetros) es solo el síntoma
del `INSERT` que intentó escribir cuando ya no había páginas nuevas.

---

## 0. Ver qué está pesando

Dos caminos, mismo dato:

```bash
# A) Desde el API (SuperAdmin). Devuelve tamaño, límite, % de uso, top de tablas y recomendaciones.
curl -H "Authorization: Bearer <token-superadmin>" \
     "https://<host>/zorvian/v1/system/db-storage?topTables=20"
```

```bash
# B) SQL directo (solo lectura) en la consola de Neon o con psql.
psql "<connection-string>" -f scripts/db_storage_report.sql
```

El script devuelve: tamaño de la base y `neon.max_cluster_size`, top 25 tablas por
`pg_total_relation_size`, columnas de texto grandes, índices más pesados y filas muertas
por tabla.

Interpretación rápida:

| Señal | Significado | Acción |
| --- | --- | --- |
| `usage_percent` ≈ 100 | Neon rechaza escrituras | Pasos 1 → 2 |
| `dead_ratio` alto (≥ 0,25) y muchos `dead_rows` | Espacio ocupado por versiones muertas: **reutilizable**, no devuelto al disco | Paso 2 |
| Una tabla domina (> 40% de la base) | Esa tabla es el problema | Revisar su retención |
| `DocumentVersions` / `GeneratedDocuments` grandes | HTML renderizado guardado en la BD | Paso 1 (el barrido a Storage ya es automático) |

---

## 1. Liberar espacio sin riesgo (purga online)

Esto borra **solo datos vencidos** según `DataRetentionRules`
(`src/Zorvian.Infrastructure/Data/DataRetentionRules.cs`) y después corre
`VACUUM ANALYZE` en las tablas tocadas:

| Tabla | Retención | Contenido |
| --- | --- | --- |
| `GpsPositions` | 90 días | Telemetría (la que más crece en flotas) |
| `WebhookDeliveryLogs` | 30 días | Entregas de webhooks + payload |
| `PalmTrackWebhookLogs` | 30 días | Payload de eventos de la integración |
| `EntityHistories` | 180 días | Historial campo a campo |
| `SyncJournals` | 90 días | Journal de sincronización offline |
| `RefreshTokens` | 30 días tras `ExpiresAt` | Tokens vencidos |
| `PasswordResetTokens` | 30 días | Tokens usados/vencidos |
| `AuditLogs` | 180 días | Job aparte: `audit-log-cleanup` |

```bash
# Ejecutar ahora (idempotente; batchSize opcional, default 5000)
curl -X POST -H "Authorization: Bearer <token-superadmin>" \
     "https://<host>/zorvian/v1/system/db-storage/purge?batchSize=5000"
```

También corre solo, todos los días, el job de Hangfire **`data-retention`** (01:30 UTC).
La retención por tabla se puede ajustar por variable de entorno sin recompilar:

```bash
Database__Retention__GpsPositions=30
Database__Retention__AuditLogs=90
```

> **Importante:** un `DELETE` **no** reduce el tamaño del proyecto en Neon. Libera páginas
> *dentro* de la tabla (se reutilizan para nuevas filas, eso desbloquea escrituras) pero el
> archivo sigue ocupando lo mismo. Para bajar el porcentaje de uso hay que reescribir la
> tabla: paso 2.

Además, el HTML de los documentos ya no vive en la BD: `DocumentVersionContentSweepJob`
(`document-version-content-sweep`, cada 10 min) sube `DocumentVersions.Content` a Storage
y vacía la columna. Se encadena solo hasta drenar la cola.

---

## 2. Devolver el espacio al sistema de archivos

`VACUUM FULL` reescribe la tabla en un archivo nuevo y borra el viejo. **Necesita espacio
libre temporal** (≈ el tamaño de la tabla resultante): con la base al 100% puede fallar con
el mismo `53100`. Estrategias, de la más barata a la más agresiva:

### 2.1 `TRUNCATE` de tablas desechables (instantáneo, no necesita espacio extra)

`TRUNCATE` crea un archivo nuevo vacío y suelta el viejo: libera el 100% del espacio de esa
tabla en segundos y sin headroom. Úsalo solo con datos que se puedan perder:

```sql
-- Logs y telemetría (el sistema las vuelve a llenar desde cero)
TRUNCATE TABLE "WebhookDeliveryLogs";
TRUNCATE TABLE "PalmTrackWebhookLogs";
TRUNCATE TABLE "SyncJournals";
TRUNCATE TABLE "GpsPositions";        -- histórico de posiciones GPS
TRUNCATE TABLE "EntityHistories";     -- historial de cambios (solo auditoría fina)
TRUNCATE TABLE "RefreshTokens";       -- cierra todas las sesiones: los usuarios re-loguean
TRUNCATE TABLE "PasswordResetTokens";

-- Bajo decisión del negocio (auditoría legal): perder esto NO es reversible
-- TRUNCATE TABLE "AuditLogs";
```

`DocumentVersions` **no** se trunca: los PDF/constancias y la verificación por QR leen el
HTML resuelto (Storage o `Content`). Si ya está migrado, se compacta con `VACUUM FULL`.

### 2.2 `VACUUM FULL` de las tablas que quedaron grandes pero vacías

Después de purgar/migrar, el espacio muerto sigue ocupando. Con headroom suficiente:

```sql
VACUUM FULL (ANALYZE) "DocumentVersions"   -- HTML ya movido a Storage
VACUUM FULL (ANALYZE) "AuditLogs"
VACUUM FULL (ANALYZE) "GeneratedDocuments"
VACUUM FULL (ANALYZE) "EntityHistories"
-- Hangfire guarda historial de jobs en su propio esquema
VACUUM FULL (ANALYZE) hangfire.job;
VACUUM FULL (ANALYZE) hangfire.state;
```

Si `VACUUM FULL` falla con `53100`, no hay headroom: sigue con 2.3.

### 2.3 Sin headroom: copiar a un proyecto limpio

La vía garantizada cuando ya no cabe ni la reescritura:

```bash
# 1) Exportar (comprime y no escribe en la base)
pg_dump "<connection-string-actual>" --format=custom --file=zorvian.dump --no-owner

# 2) Restaurar en un proyecto/rama nuevo de Neon (con espacio)
pg_restore --dbname "<connection-string-nuevo>" --no-owner --clean zorvian.dump

# 3) Apuntar la variable de entorno ConnectionStrings__ZorvianDb al proyecto nuevo
#    y redeployar en Render.
```

El restore solo escribe filas vivas, así que el dump/restore **compacta** de paso. Alternativa
inmediata si el negocio no puede esperar: subir temporalmente el plan de Neon (con más
límite) para poder correr `VACUUM FULL`, y volver al plan anterior después.

---

## 3. Verificación

1. `GET /zorvian/v1/system/db-storage` → `usagePercent` por debajo del 90%.
2. `POST /zorvian/v1/system/db-storage/purge` → `totalRowsDeleted` coherente y sin `notes` de error.
3. Crear algo que escriba (una venta de prueba, un check-in) → 200/201, no 503.
4. Log de Render: sin `53100`; el arranque no repite `Failed executing DbCommand` en cada seeder.

---

## 4. Qué se cambió en el código para que no vuelva a pasar

| Protección | Dónde |
| --- | --- |
| El HTML de cada versión de documento va a Storage; la BD guarda la ruta (`Content` queda vacío, `FilePath`/`FileSizeBytes`/`FileHash` persisten) | `DocumentService.GenerateProfessionalDocumentAsync` |
| Barrido de las versiones antiguas, en lotes de 50, encadenado hasta drenar | `DocumentVersionContentSweepJob` |
| El seeder de plantillas ya no reescribe ~1 MB de HTML en cada arranque (compara antes de escribir) | `DocumentTemplateSeeder.UpgradeSeededTemplatesAsync` |
| Un fallo de seeder no tumba el arranque, y si es por falta de espacio se omiten los restantes | `Program.cs` → `SeedSafelyAsync` |
| Purga diaria de tablas de alto volumen + `VACUUM ANALYZE` | `DataRetentionJob` (`data-retention`) |
| Purga de auditoría en lotes acotados (antes cargaba todo lo vencido en memoria) | `AuditLogCleanupJob` |
| `53100` se traduce a **503 con mensaje accionable** en vez de 500 genérico | `GlobalExceptionMiddleware` |
| Reporte y purga bajo demanda desde el API | `GET/POST /zorvian/v1/system/db-storage[/purge]` |

---

## 5. Notas

- Los endpoints de mantenimiento exigen `company.manage` (SuperAdmin) y **no** llevan
  `[Audit]`: escribir auditoría fallaría justo cuando la base está llena.
- La purga se ejecuta con la sesión de PostgreSQL del request: si un SuperAdmin tiene una
  compañía seleccionada, las políticas RLS acotan el borrado a ese tenant. El job de
  Hangfire corre sin tenant y barre todo (el escape de contexto de RLS está documentado en
  `docs/SECURITY_RLS.sql`).
- `VACUUM FULL` toma un lock exclusivo de la tabla (bloquea escrituras mientras corre). En
  horario de oficina, hágalo por tabla, de la más grande a la más chica.
- `DatabaseBackupJob` escribe el dump en el disco efímero de Render, no en la base: no
  consume el límite de Neon (y se pierde en cada deploy).
