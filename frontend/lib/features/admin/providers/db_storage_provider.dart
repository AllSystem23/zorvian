import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/auth/auth_provider.dart';

/// Uso de disco de una tabla (tabla + índices + TOAST) y salud de sus filas.
///
/// Espejo de `DatabaseTableUsage` del backend (SystemController →
/// `IDatabaseMaintenanceService.GetStorageReportAsync`).
class DatabaseTableUsage {
  final String tableName;
  final int totalBytes;
  final int tableBytes;
  final int indexBytes;
  final int liveRows;
  final int deadRows;
  final double totalMb;
  final double deadRowRatio;

  const DatabaseTableUsage({
    required this.tableName,
    required this.totalBytes,
    required this.tableBytes,
    required this.indexBytes,
    required this.liveRows,
    required this.deadRows,
    required this.totalMb,
    required this.deadRowRatio,
  });

  factory DatabaseTableUsage.fromJson(Map<String, dynamic> json) =>
      DatabaseTableUsage(
        tableName: json['tableName'] as String? ?? '',
        totalBytes: (json['totalBytes'] as num?)?.toInt() ?? 0,
        tableBytes: (json['tableBytes'] as num?)?.toInt() ?? 0,
        indexBytes: (json['indexBytes'] as num?)?.toInt() ?? 0,
        liveRows: (json['liveRows'] as num?)?.toInt() ?? 0,
        deadRows: (json['deadRows'] as num?)?.toInt() ?? 0,
        totalMb: (json['totalMb'] as num?)?.toDouble() ?? 0,
        deadRowRatio: (json['deadRowRatio'] as num?)?.toDouble() ?? 0,
      );
}

/// Foto del almacenamiento de la base de datos: tamaño, límite del plan (Neon
/// expone `neon.max_cluster_size`), uso en %, tablas que más pesan y
/// recomendaciones accionables.
class DatabaseStorageReport {
  final String provider;
  final String databaseName;
  final int databaseSizeBytes;
  final int? limitBytes;
  final double? usagePercent;
  final double warningThresholdPercent;
  final bool isStorageFull;
  final DateTime? generatedAtUtc;
  final List<DatabaseTableUsage> largestTables;
  final List<String> recommendations;

  const DatabaseStorageReport({
    required this.provider,
    required this.databaseName,
    required this.databaseSizeBytes,
    required this.limitBytes,
    required this.usagePercent,
    required this.warningThresholdPercent,
    required this.isStorageFull,
    required this.generatedAtUtc,
    required this.largestTables,
    required this.recommendations,
  });

  /// El reporte solo existe en PostgreSQL (Neon); en otros proveedores el
  /// backend devuelve un reporte vacío con la nota de "no disponible".
  bool get isAvailable => limitBytes != null || largestTables.isNotEmpty;

  /// ≥ umbral de aviso (90% por defecto). `isStorageFull` es el caso extremo.
  bool get isWarning =>
      !isStorageFull && usagePercent != null && usagePercent! >= warningThresholdPercent;

  factory DatabaseStorageReport.fromJson(Map<String, dynamic> json) =>
      DatabaseStorageReport(
        provider: json['provider'] as String? ?? '',
        databaseName: json['databaseName'] as String? ?? '',
        databaseSizeBytes: (json['databaseSizeBytes'] as num?)?.toInt() ?? 0,
        limitBytes: (json['limitBytes'] as num?)?.toInt(),
        usagePercent: (json['usagePercent'] as num?)?.toDouble(),
        warningThresholdPercent:
            (json['warningThresholdPercent'] as num?)?.toDouble() ?? 90,
        isStorageFull: json['isStorageFull'] as bool? ?? false,
        generatedAtUtc: DateTime.tryParse(json['generatedAtUtc'] as String? ?? ''),
        largestTables: (json['largestTables'] as List?)
                ?.map((e) =>
                    DatabaseTableUsage.fromJson(Map<String, dynamic>.from(e as Map)))
                .toList() ??
            const [],
        recommendations: (json['recommendations'] as List?)
                ?.map((e) => e.toString())
                .toList() ??
            const [],
      );
}

/// Resultado de purgar una tabla según su política de retención.
class RetentionPurgeResult {
  final String tableName;
  final String label;
  final int retentionDays;
  final DateTime? cutoffUtc;
  final int rowsDeleted;
  final int sizeBeforeBytes;
  final int sizeAfterBytes;

  const RetentionPurgeResult({
    required this.tableName,
    required this.label,
    required this.retentionDays,
    required this.cutoffUtc,
    required this.rowsDeleted,
    required this.sizeBeforeBytes,
    required this.sizeAfterBytes,
  });

  factory RetentionPurgeResult.fromJson(Map<String, dynamic> json) =>
      RetentionPurgeResult(
        tableName: json['tableName'] as String? ?? '',
        label: json['label'] as String? ?? '',
        retentionDays: (json['retentionDays'] as num?)?.toInt() ?? 0,
        cutoffUtc: DateTime.tryParse(json['cutoffUtc'] as String? ?? ''),
        rowsDeleted: (json['rowsDeleted'] as num?)?.toInt() ?? 0,
        sizeBeforeBytes: (json['sizeBeforeBytes'] as num?)?.toInt() ?? 0,
        sizeAfterBytes: (json['sizeAfterBytes'] as num?)?.toInt() ?? 0,
      );
}

/// Resumen de una corrida de retención (botón "Purgar" o job `data-retention`).
class DataRetentionPurgeSummary {
  final DateTime? executedAtUtc;
  final int totalRowsDeleted;
  final int totalSizeBeforeBytes;
  final int totalSizeAfterBytes;
  final List<RetentionPurgeResult> results;
  final List<String> notes;

  const DataRetentionPurgeSummary({
    required this.executedAtUtc,
    required this.totalRowsDeleted,
    required this.totalSizeBeforeBytes,
    required this.totalSizeAfterBytes,
    required this.results,
    required this.notes,
  });

  /// Tablas donde efectivamente se borró algo.
  List<RetentionPurgeResult> get purgedTables =>
      results.where((r) => r.rowsDeleted > 0).toList();

  factory DataRetentionPurgeSummary.fromJson(Map<String, dynamic> json) =>
      DataRetentionPurgeSummary(
        executedAtUtc:
            DateTime.tryParse(json['executedAtUtc'] as String? ?? ''),
        totalRowsDeleted: (json['totalRowsDeleted'] as num?)?.toInt() ?? 0,
        totalSizeBeforeBytes:
            (json['totalSizeBeforeBytes'] as num?)?.toInt() ?? 0,
        totalSizeAfterBytes:
            (json['totalSizeAfterBytes'] as num?)?.toInt() ?? 0,
        results: (json['results'] as List?)
                ?.map((e) =>
                    RetentionPurgeResult.fromJson(Map<String, dynamic>.from(e as Map)))
                .toList() ??
            const [],
        notes: (json['notes'] as List?)?.map((e) => e.toString()).toList() ??
            const [],
      );
}

/// Resultado de vaciar (`TRUNCATE`) una tabla en una purga de emergencia.
///
/// Espejo de `RetentionTruncateResult`: no hay "filas vencidas", se descarta
/// todo el contenido de una tabla de telemetría/logs.
class RetentionTruncateResult {
  final String tableName;
  final String label;
  final int sizeBeforeBytes;
  final int sizeAfterBytes;

  const RetentionTruncateResult({
    required this.tableName,
    required this.label,
    required this.sizeBeforeBytes,
    required this.sizeAfterBytes,
  });

  factory RetentionTruncateResult.fromJson(Map<String, dynamic> json) =>
      RetentionTruncateResult(
        tableName: json['tableName'] as String? ?? '',
        label: json['label'] as String? ?? '',
        sizeBeforeBytes: (json['sizeBeforeBytes'] as num?)?.toInt() ?? 0,
        sizeAfterBytes: (json['sizeAfterBytes'] as num?)?.toInt() ?? 0,
      );
}

/// Resumen de una purga de EMERGENCIA (`POST system/db-storage/truncate`).
///
/// Se usa cuando la base ya está en el límite del plan y ni un `DELETE` pasa
/// (Neon responde `53100` también al borrar). `TRUNCATE` vacía el archivo sin
/// pedir espacio, así que es la única operación que desbloquea las escrituras.
class DataRetentionTruncateSummary {
  final DateTime? executedAtUtc;
  final int totalSizeBeforeBytes;
  final int totalSizeAfterBytes;
  final List<RetentionTruncateResult> results;
  final List<String> notes;

  const DataRetentionTruncateSummary({
    required this.executedAtUtc,
    required this.totalSizeBeforeBytes,
    required this.totalSizeAfterBytes,
    required this.results,
    required this.notes,
  });

  /// Tablas que efectivamente quedaron vacías.
  int get tablesEmptied => results.length;

  factory DataRetentionTruncateSummary.fromJson(Map<String, dynamic> json) =>
      DataRetentionTruncateSummary(
        executedAtUtc: DateTime.tryParse(json['executedAtUtc'] as String? ?? ''),
        totalSizeBeforeBytes:
            (json['totalSizeBeforeBytes'] as num?)?.toInt() ?? 0,
        totalSizeAfterBytes:
            (json['totalSizeAfterBytes'] as num?)?.toInt() ?? 0,
        results: (json['results'] as List?)
                ?.map((e) => RetentionTruncateResult.fromJson(
                    Map<String, dynamic>.from(e as Map)))
                .toList() ??
            const [],
        notes: (json['notes'] as List?)?.map((e) => e.toString()).toList() ??
            const [],
      );
}

/// Reporte de almacenamiento de la base de datos (endpoint plataforma
/// `GET system/db-storage`, permiso `company.manage`).
/// No observa `branchScopeProvider`: el reporte es de toda la base (tamaño de
/// la BD no tiene dimensión de sucursal).
class DbStorageNotifier extends AsyncNotifier<DatabaseStorageReport> {
  @override
  Future<DatabaseStorageReport> build() => _fetch();

  Future<void> load() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(_fetch);
  }

  Future<DatabaseStorageReport> _fetch() async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.get(
      'system/db-storage',
      params: {'topTables': 15},
    );
    return DatabaseStorageReport.fromJson(
      Map<String, dynamic>.from(response.data as Map),
    );
  }

  /// Ejecuta AHORA la purga de retención (`POST system/db-storage/purge`) y
  /// recarga el reporte. Devuelve el resumen para que la página muestre el
  /// detalle (filas borradas por tabla + notas del backend).
  Future<DataRetentionPurgeSummary> purge() async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.post('system/db-storage/purge');
    final summary = DataRetentionPurgeSummary.fromJson(
      Map<String, dynamic>.from(response.data as Map),
    );
    // La purga cambia tamaños y filas: el reporte se refresca solo.
    state = await AsyncValue.guard(_fetch);
    return summary;
  }

  /// Purga de EMERGENCIA (`POST system/db-storage/truncate`): vacía tablas de
  /// telemetría/logs cuando la base ya está en el límite del plan y ni un
  /// `DELETE` pasa. Descarta TODO el contenido de las tablas afectadas.
  ///
  /// [tables] vacío = todas las tablas de retención permitidas. [includeAuditLogs]
  /// habilita `AuditLogs` (se conserva 6 meses por diseño, AUD-004).
  Future<DataRetentionTruncateSummary> truncate({
    List<String> tables = const [],
    bool includeAuditLogs = false,
  }) async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.post(
      'system/db-storage/truncate',
      queryParameters: {
        if (tables.isNotEmpty) 'tables': tables.join(','),
        'includeAuditLogs': includeAuditLogs,
      },
    );
    final summary = DataRetentionTruncateSummary.fromJson(
      Map<String, dynamic>.from(response.data as Map),
    );
    // El vaciado cambia tamaños y filas: el reporte se refresca solo.
    state = await AsyncValue.guard(_fetch);
    return summary;
  }
}

final dbStorageProvider =
    AsyncNotifierProvider<DbStorageNotifier, DatabaseStorageReport>(
        DbStorageNotifier.new);
