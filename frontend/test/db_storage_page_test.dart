import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/admin/pages/db_storage_page.dart';
import 'package:zorvian/features/admin/providers/db_storage_provider.dart';
import 'package:zorvian/shared/ds/ds.dart';

const _mb = 1024 * 1024;

/// Forma real de `DatabaseStorageReport` (SystemController → GET system/db-storage).
Map<String, dynamic> _report({bool full = false}) => {
      'provider': 'Npgsql.EntityFrameworkCore.PostgreSQL',
      'databaseName': 'neondb',
      'databaseSizeBytes': 1024 * _mb,
      'limitBytes': 1024 * _mb,
      'usagePercent': full ? 100.0 : 93.5,
      'warningThresholdPercent': 90.0,
      'isStorageFull': full,
      'generatedAtUtc': '2026-10-28T10:00:00Z',
      'largestTables': [
        {
          'tableName': 'DocumentVersions',
          'totalBytes': 400 * _mb,
          'tableBytes': 380 * _mb,
          'indexBytes': 20 * _mb,
          'liveRows': 1200,
          'deadRows': 300,
          'totalMb': 400.0,
          'deadRowRatio': 0.2,
        },
      ],
      'recommendations': [
        'La base está al ${full ? '100' : '93,5'}% de su límite (1024 MB). '
            'Ejecute la purga de retención y planifique VACUUM FULL antes de llegar al 100%.',
      ],
    };

/// Reporte del camino "sin PostgreSQL": el backend lo marca como no disponible.
Map<String, dynamic> _unavailableReport() => {
      'provider': 'Microsoft.EntityFrameworkCore.InMemory',
      'databaseName': 'Microsoft.EntityFrameworkCore.InMemory',
      'databaseSizeBytes': 0,
      'limitBytes': null,
      'usagePercent': null,
      'warningThresholdPercent': 90.0,
      'isStorageFull': false,
      'generatedAtUtc': '2026-10-28T10:00:00Z',
      'largestTables': <dynamic>[],
      'recommendations': [
        'El reporte de almacenamiento solo está disponible en PostgreSQL (Neon).'
      ],
    };

Map<String, dynamic> _purgeSummary({int rows = 1234}) => {
      'executedAtUtc': '2026-10-28T10:05:00Z',
      'totalRowsDeleted': rows,
      'totalSizeBeforeBytes': 500 * _mb,
      'totalSizeAfterBytes': 500 * _mb,
      'results': [
        {
          'tableName': 'GpsPositions',
          'label': 'Posiciones GPS',
          'retentionDays': 90,
          'cutoffUtc': '2026-07-30T10:05:00Z',
          'rowsDeleted': rows,
          'sizeBeforeBytes': 500 * _mb,
          'sizeAfterBytes': 500 * _mb,
        },
      ],
      'notes': [
        'Un DELETE no reduce el tamaño del proyecto en Neon: el espacio queda reutilizable. '
            'Para recuperarlo ejecute VACUUM FULL (docs/DB_STORAGE_RECOVERY.md) o amplíe el plan.',
      ],
    };

ResponseBody _json(Object? data, [int status = 200]) => ResponseBody.fromString(
      jsonEncode(data),
      status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

/// `SecureStorage` de prueba: el canal del plugin nunca responde dentro del reloj
/// falso de `testWidgets`, así que el token se sirve desde memoria.
class _TestStorage extends SecureStorage {
  @override
  Future<String?> getAccessToken() async => 'test-token';

  @override
  Future<String?> getRefreshToken() async => null;
}

class _FakeAdapter implements HttpClientAdapter {
  _FakeAdapter(this._handle);

  final Future<ResponseBody> Function(RequestOptions options) _handle;
  final List<RequestOptions> requests = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    return _handle(options);
  }

  @override
  void close({bool force = false}) {}
}

class _FakeApi {
  _FakeApi({required this.report, this.purgeSummary});

  final Map<String, dynamic> report;
  final Map<String, dynamic>? purgeSummary;
  late final _FakeAdapter adapter = _FakeAdapter(_handle);

  int getCalls = 0;
  int postCalls = 0;

  Future<ResponseBody> _handle(RequestOptions options) async {
    if (options.method == 'POST' && options.path == 'system/db-storage/purge') {
      postCalls++;
      return _json(purgeSummary ?? _purgeSummary());
    }
    if (options.path == 'system/db-storage') {
      getCalls++;
      return _json(report);
    }
    return _json({'error': 'no encontrado'}, 404);
  }
}

Future<_FakeApi> _pumpPage(
  WidgetTester tester, {
  Map<String, dynamic>? report,
  Map<String, dynamic>? purgeSummary,
}) async {
  tester.view.physicalSize = const Size(1400, 2000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  final api = _FakeApi(
    report: report ?? _report(),
    purgeSummary: purgeSummary,
  );
  final client = DioClient(_TestStorage(), adapter: api.adapter);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(client)],
      child: const MaterialApp(home: DbStoragePage()),
    ),
  );
  await tester.pumpAndSettle();
  return api;
}

void main() {
  group('modelos', () {
    test('DatabaseStorageReport mapea el payload del backend', () {
      final report = DatabaseStorageReport.fromJson(_report());

      expect(report.databaseName, 'neondb');
      expect(report.limitBytes, 1024 * _mb);
      expect(report.usagePercent, 93.5);
      expect(report.isStorageFull, isFalse);
      expect(report.isWarning, isTrue);
      expect(report.isAvailable, isTrue);
      expect(report.largestTables, hasLength(1));
      expect(report.largestTables.first.tableName, 'DocumentVersions');
      expect(report.largestTables.first.deadRowRatio, 0.2);
      expect(report.generatedAtUtc, isNotNull);
    });

    test('isStorageFull gana sobre isWarning y el reporte vacío no está disponible', () {
      expect(DatabaseStorageReport.fromJson(_report(full: true)).isStorageFull, isTrue);
      expect(DatabaseStorageReport.fromJson(_report(full: true)).isWarning, isFalse);

      final unavailable = DatabaseStorageReport.fromJson(_unavailableReport());
      expect(unavailable.isAvailable, isFalse);
      expect(unavailable.usagePercent, isNull);
      expect(unavailable.largestTables, isEmpty);
    });

    test('DataRetentionPurgeSummary expone las tablas efectivamente purgadas', () {
      final summary = DataRetentionPurgeSummary.fromJson(_purgeSummary());

      expect(summary.totalRowsDeleted, 1234);
      expect(summary.purgedTables, hasLength(1));
      expect(summary.purgedTables.first.tableName, 'GpsPositions');
      expect(summary.purgedTables.first.retentionDays, 90);
      expect(summary.notes.single, contains('VACUUM FULL'));
    });
  });

  group('página de almacenamiento', () {
    testWidgets('muestra el % de uso, el aviso y las tablas más grandes', (tester) async {
      final api = await _pumpPage(tester);

      expect(api.getCalls, 1);
      expect(find.text('Almacenamiento de Base de Datos'), findsOneWidget);
      expect(find.textContaining('(93.5%)'), findsOneWidget);
      expect(find.text('CERCA DEL LÍMITE'), findsOneWidget);
      expect(find.text('DocumentVersions'), findsOneWidget);
      expect(find.text('400.00 MB'), findsOneWidget);
      // 20% de filas muertas: el reporte las muestra con un decimal.
      expect(find.text('20.0%'), findsOneWidget);
      expect(find.textContaining('VACUUM FULL'), findsWidgets);
    });

    testWidgets('base llena: badge LLENO y mensaje accionable sobre 53100', (tester) async {
      await _pumpPage(tester, report: _report(full: true));

      expect(find.text('LLENO'), findsOneWidget);
      expect(find.textContaining('53100'), findsOneWidget);
      expect(find.textContaining('amplíe el plan'), findsOneWidget);
    });

    testWidgets('sin PostgreSQL avisa que el reporte no está disponible', (tester) async {
      await _pumpPage(tester, report: _unavailableReport());

      expect(find.textContaining('solo está disponible en PostgreSQL'), findsWidgets);
      expect(find.textContaining('el proveedor no expone un límite'), findsOneWidget);
      // Sin tablas no se pinta la ZDataTable.
      expect(find.byType(ZDataTable<DatabaseTableUsage>), findsNothing);
    });

    testWidgets('purgar pide confirmación, hace POST y refresca el reporte', (tester) async {
      final api = await _pumpPage(tester);

      await tester.tap(find.widgetWithText(ZButton, 'Purgar datos vencidos'));
      await tester.pumpAndSettle();

      // El diálogo explica qué se borra y qué NO se toca.
      expect(find.text('Purgar datos vencidos'), findsWidgets);
      expect(find.textContaining('NO se tocan'), findsOneWidget);

      await tester.tap(find.widgetWithText(ZButton, 'PURGAR'));
      await tester.pumpAndSettle();

      expect(api.postCalls, 1);
      // POST + recarga del reporte.
      expect(api.getCalls, 2);
      expect(find.textContaining('1.234 fila(s) eliminadas'), findsOneWidget);
      expect(find.textContaining('VACUUM FULL'), findsWidgets);

      // Dejar expirar el SnackBar antes del teardown (timers pendientes).
      await tester.pump(const Duration(seconds: 6));
    });

    testWidgets('cancelar la confirmación no purga nada', (tester) async {
      final api = await _pumpPage(tester);

      await tester.tap(find.widgetWithText(ZButton, 'Purgar datos vencidos'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(api.postCalls, 0);
      expect(api.getCalls, 1);
    });

    testWidgets('purga sin datos vencidos informa que todo está dentro de retención', (tester) async {
      final api = await _pumpPage(
        tester,
        purgeSummary: {
          ...(_purgeSummary(rows: 0)),
          'results': <dynamic>[],
        },
      );

      await tester.tap(find.widgetWithText(ZButton, 'Purgar datos vencidos'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(ZButton, 'PURGAR'));
      await tester.pumpAndSettle();

      expect(api.postCalls, 1);
      expect(find.textContaining('No había datos vencidos'), findsOneWidget);
      await tester.pump(const Duration(seconds: 6));
    });
  });
}
