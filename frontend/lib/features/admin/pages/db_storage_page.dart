import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/shared/ds/ds.dart';

import '../providers/db_storage_provider.dart';

/// Diagnóstico y mantenimiento del almacenamiento de la base de datos.
///
/// Nació del incidente de Neon (proyecto de 1024 MB lleno → toda escritura fallaba
/// con `53100: could not extend file because project size limit (1024 MB) has been
/// exceeded`). Muestra qué tablas ocupan el espacio, el % de uso del plan y permite
/// ejecutar la purga de retención sin abrir psql.
///
/// Es una pantalla de plataforma (SuperAdmin): los endpoints `system/db-storage`
/// exigen `company.manage`. `VACUUM FULL` (devolver el espacio al sistema de
/// archivos) NO está aquí a propósito: necesita headroom y bloquea la tabla — vive
/// en el runbook `docs/DB_STORAGE_RECOVERY.md`.
class DbStoragePage extends ConsumerStatefulWidget {
  const DbStoragePage({super.key});

  @override
  ConsumerState<DbStoragePage> createState() => _DbStoragePageState();
}

class _DbStoragePageState extends ConsumerState<DbStoragePage> {
  bool _purging = false;
  bool _truncating = false;

  @override
  Widget build(BuildContext context) {
    final report = ref.watch(dbStorageProvider);

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Wrap(
              spacing: ZSpacing.sm,
              runSpacing: ZSpacing.sm,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                const Text(
                  'Almacenamiento de Base de Datos',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                ),
                ZButton(
                  text: 'Actualizar',
                  icon: Icons.refresh_rounded,
                  fullWidth: false,
                  type: ZButtonType.secondary,
                  onPressed: () => ref.read(dbStorageProvider.notifier).load(),
                ),
                ZButton(
                  text: 'Purgar datos vencidos',
                  icon: Icons.cleaning_services_outlined,
                  fullWidth: false,
                  type: ZButtonType.danger,
                  isLoading: _purging,
                  // ZButton.onPressed no es nullable: se deshabilita con un no-op.
                  onPressed: _purging || _truncating ? () {} : _confirmPurge,
                ),
                ZButton(
                  text: 'Purga de emergencia (TRUNCATE)',
                  icon: Icons.warning_amber_rounded,
                  fullWidth: false,
                  type: ZButtonType.danger,
                  isLoading: _truncating,
                  onPressed: _purging || _truncating ? () {} : _confirmTruncate,
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.md),
            report.when(
              loading: () => const Center(
                child: Padding(
                  padding: EdgeInsets.all(ZSpacing.xl),
                  child: CircularProgressIndicator(),
                ),
              ),
              error: (error, _) => _errorCard(error),
              data: (data) => _reportBody(data),
            ),
          ],
        ),
      ),
    );
  }

  Widget _errorCard(Object error) {
    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.error_outline, color: ZColors.danger),
              SizedBox(width: 8),
              Text(
                'No se pudo leer el almacenamiento de la base de datos',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            '$error',
            style: const TextStyle(fontSize: 12, color: ZColors.neutral500),
          ),
          const SizedBox(height: 12),
          ZButton(
            text: 'Reintentar',
            fullWidth: false,
            onPressed: () => ref.read(dbStorageProvider.notifier).load(),
          ),
        ],
      ),
    );
  }

  Widget _reportBody(DatabaseStorageReport report) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _usageCard(report),
        const SizedBox(height: ZSpacing.md),
        if (report.recommendations.isNotEmpty) ...[
          _recommendationsCard(report),
          const SizedBox(height: ZSpacing.md),
        ],
        _tablesCard(report),
      ],
    );
  }

  Widget _usageCard(DatabaseStorageReport report) {
    final percent = report.usagePercent;
    final ratio = percent == null ? null : (percent / 100).clamp(0.0, 1.0);
    final color = report.isStorageFull
        ? ZColors.danger
        : report.isWarning
            ? ZColors.warning
            : ZColors.success;

    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text(
                  'Uso del proyecto',
                  style: TextStyle(fontWeight: FontWeight.w600),
                ),
              ),
              if (report.isStorageFull)
                const ZBadge(text: 'LLENO', type: ZBadgeType.danger)
              else if (report.isWarning)
                const ZBadge(text: 'CERCA DEL LÍMITE', type: ZBadgeType.warning)
              else if (report.isAvailable)
                const ZBadge(text: 'SALUDABLE', type: ZBadgeType.success),
            ],
          ),
          const SizedBox(height: 12),
          if (ratio != null) ...[
            ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: LinearProgressIndicator(
                value: ratio,
                minHeight: 10,
                backgroundColor: ZColors.neutral200,
                valueColor: AlwaysStoppedAnimation<Color>(color),
              ),
            ),
            const SizedBox(height: 8),
            Text(
              '${_mb(report.databaseSizeBytes)} de ${_mb(report.limitBytes ?? 0)} '
              '(${percent!.toStringAsFixed(1)}%)',
              style: const TextStyle(fontSize: 13),
            ),
          ] else
            Text(
              'Tamaño actual: ${_mb(report.databaseSizeBytes)} '
              '(el proveedor no expone un límite de plan).',
              style: const TextStyle(fontSize: 13),
            ),
          const SizedBox(height: 4),
          Text(
            'Base: ${report.databaseName} · Proveedor: ${report.provider}',
            style: const TextStyle(fontSize: 11, color: ZColors.neutral500),
          ),
          if (report.isStorageFull) ...[
            const SizedBox(height: 12),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(ZSpacing.sm),
              decoration: BoxDecoration(
                color: ZColors.danger.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: ZColors.danger.withValues(alpha: 0.3)),
              ),
              child: const Text(
                'La base alcanzó el límite del plan y PostgreSQL rechaza las '
                'escrituras (53100): no se pueden guardar ventas, nóminas ni '
                'documentos. Ejecute la purga; si el % no baja, siga '
                'docs/DB_STORAGE_RECOVERY.md (TRUNCATE / VACUUM FULL / dump+restore) '
                'o amplíe el plan.',
                style: TextStyle(fontSize: 12),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _recommendationsCard(DatabaseStorageReport report) {
    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Recomendaciones',
            style: TextStyle(fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 8),
          ...report.recommendations.map(
            (note) => Padding(
              padding: const EdgeInsets.only(bottom: 6),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.info_outline, size: 16, color: ZColors.neutral500),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(note, style: const TextStyle(fontSize: 12)),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _tablesCard(DatabaseStorageReport report) {
    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Tablas que más ocupan',
            style: TextStyle(fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 4),
          const Text(
            'Filas muertas = versiones obsoletas que VACUUM aún no recicló '
            '(espacio reutilizable, no devuelto al disco).',
            style: TextStyle(fontSize: 11, color: ZColors.neutral500),
          ),
          const SizedBox(height: 12),
          if (report.largestTables.isEmpty)
            const Text(
              'Sin datos de tablas: el reporte de almacenamiento solo está '
              'disponible en PostgreSQL (Neon).',
              style: TextStyle(fontSize: 12, color: ZColors.neutral500),
            )
          else
            ZDataTable<DatabaseTableUsage>(
              columns: const [
                ZColumn(id: 'table', label: 'Tabla', width: 220),
                ZColumn(id: 'total', label: 'Total', width: 110),
                ZColumn(id: 'tableOnly', label: 'Datos', width: 110),
                ZColumn(id: 'indexes', label: 'Índices', width: 110),
                ZColumn(id: 'live', label: 'Filas vivas', width: 120),
                ZColumn(id: 'dead', label: 'Filas muertas', width: 130),
                ZColumn(id: 'ratio', label: '% muertas', width: 110),
              ],
              rows: report.largestTables,
              rowMapper: (item) => DataRow(cells: [
                DataCell(Text(
                  item.tableName,
                  style: const TextStyle(fontFamily: 'monospace', fontSize: 12),
                )),
                DataCell(Text(_mb(item.totalBytes))),
                DataCell(Text(_mb(item.tableBytes))),
                DataCell(Text(_mb(item.indexBytes))),
                DataCell(Text(_thousands(item.liveRows))),
                DataCell(Text(_thousands(item.deadRows))),
                DataCell(Text('${(item.deadRowRatio * 100).toStringAsFixed(1)}%')),
              ]),
            ),
        ],
      ),
    );
  }

  Future<void> _confirmPurge() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Purgar datos vencidos'),
        content: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 460),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: const [
              Text(
                'Se borrarán SOLO las filas que superaron su retención: '
                'telemetría GPS (90 d), entregas de webhooks y payloads de '
                'PalmTrack (30 d), historial de entidades (180 d), journal de '
                'sincronización (90 d) y tokens vencidos (30 d).',
                style: TextStyle(fontSize: 13),
              ),
              SizedBox(height: 12),
              Text(
                'Los datos de negocio (ventas, compras, nómina, asientos, '
                'documentos) NO se tocan. Después del borrado se ejecuta '
                'VACUUM ANALYZE para que PostgreSQL reutilice las páginas.',
                style: TextStyle(fontSize: 12, color: ZColors.neutral500),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          ZButton(
            text: 'PURGAR',
            fullWidth: false,
            type: ZButtonType.danger,
            onPressed: () => Navigator.of(dialogContext).pop(true),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

    setState(() => _purging = true);
    try {
      final summary = await ref.read(dbStorageProvider.notifier).purge();
      if (!mounted) return;

      final tables = summary.purgedTables.length;
      final message = StringBuffer();

      if (summary.totalRowsDeleted == 0) {
        message.write(
          'No había datos vencidos: todas las tablas están dentro de su retención.',
        );
      } else {
        message.write(
          '${_thousands(summary.totalRowsDeleted)} fila(s) eliminadas en '
          '$tables tabla(s).',
        );
      }

      // Las notas del backend explican lo que un DELETE NO hace (devolver el
      // tamaño al sistema de archivos). Van en el MISMO toast a propósito:
      // `ZToast.show` hace `hideCurrentSnackBar()` antes de mostrar, así que un
      // segundo aviso en la misma tanda ocultaría el primero.
      final actionable =
          summary.notes.where((n) => n.toLowerCase().contains('vacuum')).toList();
      if (actionable.isNotEmpty) message.write(' ${actionable.first}');

      ZToast.show(
        context,
        message.toString(),
        type: summary.totalRowsDeleted == 0
            ? ZToastType.info
            : ZToastType.success,
      );
    } catch (e) {
      if (mounted) {
        ZToast.error(context, 'No se pudo ejecutar la purga: $e');
      }
    } finally {
      if (mounted) setState(() => _purging = false);
    }
  }

  /// Purga de EMERGENCIA: `TRUNCATE` de las tablas de retención.
  ///
  /// Solo tiene sentido cuando la base ya está en el límite del plan: en ese
  /// momento Neon responde `53100` incluso a un `DELETE`, y `TRUNCATE` (que
  /// crea el archivo vacío sin pedir espacio) es la única operación que
  /// desbloquea las escrituras desde el API.
  Future<void> _confirmTruncate() async {
    var includeAuditLogs = false;
    var acknowledged = false;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (builderContext, setDialogState) {
          return AlertDialog(
            title: const Text('Purga de emergencia (TRUNCATE)'),
            content: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'VACÍA POR COMPLETO las tablas de telemetría, logs y '
                      'tokens, no solo las filas vencidas. Úselo solo si la base '
                      'ya está en el límite del plan y PostgreSQL rechaza las '
                      'escrituras (53100): TRUNCATE no necesita espacio libre, '
                      'así que es la única purga que desbloquea las escrituras.',
                      style: TextStyle(fontSize: 13),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Los datos de negocio (ventas, compras, nómina, asientos, '
                      'documentos) NUNCA se vacían: solo se permite sobre las '
                      'tablas de retención.',
                      style: TextStyle(fontSize: 12, color: ZColors.neutral500),
                    ),
                    const SizedBox(height: 8),
                    CheckboxListTile(
                      value: includeAuditLogs,
                      onChanged: (value) => setDialogState(
                          () => includeAuditLogs = value ?? false),
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      controlAffinity: ListTileControlAffinity.leading,
                      title: const Text(
                        'Incluir AuditLogs (borra TODO el log de auditoría)',
                        style: TextStyle(fontSize: 13),
                      ),
                      subtitle: const Text(
                        'Por diseño se conservan 6 meses (AUD-004).',
                        style: TextStyle(fontSize: 11),
                      ),
                    ),
                    CheckboxListTile(
                      value: acknowledged,
                      onChanged: (value) =>
                          setDialogState(() => acknowledged = value ?? false),
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      controlAffinity: ListTileControlAffinity.leading,
                      title: const Text(
                        'Entiendo que este vaciado es irreversible',
                        style: TextStyle(fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(dialogContext).pop(false),
                child: const Text('Cancelar'),
              ),
              ZButton(
                text: 'VACIAR',
                fullWidth: false,
                type: ZButtonType.danger,
                onPressed:
                    acknowledged ? () => Navigator.of(dialogContext).pop(true) : () {},
              ),
            ],
          );
        },
      ),
    );

    if (confirmed != true || !mounted) return;

    setState(() => _truncating = true);
    try {
      final summary = await ref
          .read(dbStorageProvider.notifier)
          .truncate(includeAuditLogs: includeAuditLogs);
      if (!mounted) return;

      final message = StringBuffer();
      if (summary.tablesEmptied == 0) {
        message.write('No se vació ninguna tabla.');
      } else {
        message.write(
            '${summary.tablesEmptied} tabla(s) vaciada(s): '
            '${summary.results.map((r) => r.tableName).join(', ')}.');
      }

      // Las notas del backend (incluida la de "TRUNCATE libera espacio de
      // inmediato") van en el MISMO toast: `ZToast.show` oculta el anterior.
      final actionable = summary.notes
          .where((n) =>
              n.toLowerCase().contains('liber') ||
              n.toLowerCase().contains('vacuum'))
          .toList();
      if (actionable.isNotEmpty) message.write(' ${actionable.first}');

      ZToast.show(
        context,
        message.toString(),
        type: summary.tablesEmptied == 0
            ? ZToastType.info
            : ZToastType.success,
      );
    } catch (e) {
      if (mounted) {
        ZToast.error(context, 'No se pudo ejecutar la purga de emergencia: $e');
      }
    } finally {
      if (mounted) setState(() => _truncating = false);
    }
  }

  static String _mb(int bytes) {
    if (bytes <= 0) return '0 MB';
    final mb = bytes / (1024 * 1024);
    if (mb < 1024) return '${mb.toStringAsFixed(2)} MB';
    return '${(mb / 1024).toStringAsFixed(2)} GB';
  }

  static String _thousands(int value) {
    final digits = value.abs().toString();
    final buffer = StringBuffer();
    for (var i = 0; i < digits.length; i++) {
      if (i > 0 && (digits.length - i) % 3 == 0) buffer.write('.');
      buffer.write(digits[i]);
    }
    return value < 0 ? '-$buffer' : buffer.toString();
  }
}
