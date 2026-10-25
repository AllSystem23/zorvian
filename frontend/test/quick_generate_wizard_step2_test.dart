/// Regresión del Centro Documental → "Liquidación Final": el paso 2 ("Confirma
/// los datos") solo mostraba 12 claves de solo lectura y `quick-generate` se
/// generaba con las ~50 variables de liquidación vacías. Ahora el paso 2 pinta
/// un formulario editable precargado con el contexto de la entidad y sus
/// ediciones viajan en `variables`.
library;

import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/documents/pages/quick_generate_wizard_page.dart';
import 'package:zorvian/shared/ds/ds.dart';

const _entityId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';
const _templateId = '11111111-2222-3333-4444-555555555555';

/// Subconjunto representativo de las variables de la plantilla "Liquidación
/// Final" (incluye las 3 obligatorias que la plantilla real declara).
const _templateVariables = [
  {
    'key': 'employee_name',
    'label': 'Nombre del Trabajador',
    'type': 'text',
    'required': true,
  },
  {
    'key': 'neto_a_recibir',
    'label': 'Neto a Recibir',
    'type': 'number',
    'required': true,
  },
  {
    'key': 'fecha_salida',
    'label': 'Fecha de Salida',
    'type': 'date',
    'required': true,
  },
  {
    'key': 'elaborado_por',
    'label': 'Elaborado Por',
    'type': 'text',
    'required': false,
  },
];

/// Contexto que devuelve `GET documents/entity-context` para un empleado
/// (claves calculadas por `CertificateCalculator` + claves básicas).
const _entityContext = {
  'employee_name': 'María López',
  'neto_a_recibir': '12,345.00',
  'fecha_salida': '2026-10-25',
  'autorizado_por': 'Comercial Zorvian',
  'salary': '8,500.00',
};

ResponseBody _json(Object? data, [int status = 200]) => ResponseBody.fromString(
      jsonEncode(data),
      status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

/// `SecureStorage` de prueba: el canal del plugin nunca responde dentro del
/// reloj falso de `testWidgets`, así que el token se sirve desde memoria
/// (si no, el interceptor deja el request colgado y `pumpAndSettle` expira).
class _TestStorage extends SecureStorage {
  @override
  Future<String?> getAccessToken() async => 'test-token';

  @override
  Future<String?> getRefreshToken() async => null;
}

/// Adapter de Dio que registra cada request y delega la respuesta al handler.
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
  _FakeApi() {
    adapter = _FakeAdapter(_handle);
  }

  late final _FakeAdapter adapter;
  List<RequestOptions> get requests => adapter.requests;

  RequestOptions get quickGenerate =>
      requests.lastWhere((r) => r.path == 'documents/quick-generate');

  Future<ResponseBody> _handle(RequestOptions options) async {
    if (options.path == 'documents/templates') {
      return _json({
        'items': [
          {
            'id': _templateId,
            'name': 'Liquidación Final',
            'category': 'RRHH',
            'content': '<html></html>',
            'countryCode': 'ALL',
            'module': 'Employee',
            'isActive': true,
            'version': '2.0',
            'variables': _templateVariables,
          },
        ],
        'total': 1,
      });
    }
    if (options.path == 'documents/entity-context') {
      return _json({
        'entityType': 'employee',
        'entityId': _entityId,
        'displayName': 'María López',
        'data': _entityContext,
      });
    }
    if (options.path == 'documents/quick-generate') {
      return _json({
        'documentId': '99999999-0000-0000-0000-000000000000',
        'name': 'Liquidación Final',
        'status': 'draft',
        'createdAt': '2026-10-25T10:00:00Z',
        'pdfUrl': null,
        'signatureToken': null,
      });
    }
    return _json({'error': 'no encontrado'}, 404);
  }
}

Object? _decoded(Object? data) => data is String ? jsonDecode(data) : data;

Future<_FakeApi> _pumpWizard(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1200, 1600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  final api = _FakeApi();
  final client = DioClient(_TestStorage(), adapter: api.adapter);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(client)],
      child: const MaterialApp(
        home: QuickGenerateWizardPage(
          entityType: 'employee',
          entityId: _entityId,
          entityDisplayName: 'María López',
          preselectedTemplateId: _templateId,
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return api;
}

Finder _fieldWithText(String value) => find.byWidgetPredicate(
      (w) => w is TextField && w.controller?.text == value,
    );

void main() {
  testWidgets('el paso 2 pinta el formulario de variables con el contexto precargado',
      (tester) async {
    await _pumpWizard(tester);

    expect(find.text('Paso 2: Confirma los datos'), findsOneWidget);
    expect(find.text('Datos del documento (4)'), findsOneWidget);

    // Las variables de liquidación llegan precargadas del contexto...
    expect(_fieldWithText('12,345.00'), findsOneWidget);
    expect(_fieldWithText('María López'), findsOneWidget);
    expect(_fieldWithText('2026-10-25'), findsOneWidget);

    // ...y las obligatorias van marcadas con *.
    // `ZTextField` pinta la etiqueta en MAYÚSCULAS; el campo de fecha usa un
    // `InputDecorator` propio y conserva la caja original.
    expect(find.text('NETO A RECIBIR *'), findsOneWidget);
    expect(find.text('ELABORADO POR'), findsOneWidget);
    expect(find.text('Fecha de Salida *'), findsOneWidget);

    expect(tester.takeException(), isNull);
  });

  testWidgets('las variables editadas viajan en el POST de quick-generate',
      (tester) async {
    final api = await _pumpWizard(tester);

    final neto = _fieldWithText('12,345.00');
    await tester.ensureVisible(neto);
    await tester.tap(neto);
    await tester.pump();
    await tester.enterText(neto, '15,000.00');
    await tester.pump();

    final button = find.byWidgetPredicate(
      (w) => w is ZButton && w.text.contains('Generar Documento'),
    );
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pumpAndSettle();

    expect(find.text('Documento Generado'), findsOneWidget);

    final body = _decoded(api.quickGenerate.data) as Map<String, dynamic>;
    expect(body['entityType'], 'employee');
    expect(body['templateId'], _templateId);
    final variables = body['variables'] as Map<String, dynamic>;
    // Lo editado en el formulario previo manda sobre el contexto calculado.
    expect(variables['neto_a_recibir'], '15,000.00');
    // Las no editadas conservan el valor del contexto.
    expect(variables['employee_name'], 'María López');
    expect(variables['fecha_salida'], '2026-10-25');
    expect(variables['autorizado_por'], isNull); // no declarada en la plantilla
  });

  testWidgets('un campo obligatorio vacío bloquea la generación',
      (tester) async {
    final api = await _pumpWizard(tester);

    final nombre = _fieldWithText('María López');
    await tester.ensureVisible(nombre);
    await tester.tap(nombre);
    await tester.pump();
    await tester.enterText(nombre, '');
    await tester.pump();

    final button = find.byWidgetPredicate(
      (w) => w is ZButton && w.text.contains('Generar Documento'),
    );
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pump();

    expect(
      find.textContaining('Completa los campos obligatorios'),
      findsWidgets,
    );
    expect(
      api.requests.where((r) => r.path == 'documents/quick-generate'),
      isEmpty,
    );
    expect(find.text('Documento Generado'), findsNothing);

    // Drena el toast de error para no dejar timers pendientes.
    await tester.pump(const Duration(seconds: 5));
  });
}
