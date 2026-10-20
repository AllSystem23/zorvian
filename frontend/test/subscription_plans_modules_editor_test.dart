import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/admin/pages/subscription_plans_page.dart';
import 'package:zorvian/shared/ds/ds.dart';

/// GUID de la fila `SubscriptionPlans` (el `id` que expone el API).
const _planGuid = '11111111-1111-1111-1111-111111111111';

const _starterModules = ['Sales', 'Inventory', 'Hr', 'Administration'];

/// Forma real de `SubscriptionPlanResponse`: `id` = GUID de fila y `planId` =
/// clave de negocio (lo que habilita el editor de módulos).
Map<String, dynamic> _dbPlan({List<String> modules = _starterModules}) => {
      'id': _planGuid,
      'planId': 'starter',
      'name': 'Starter',
      'price': 0,
      'period': 'Para siempre',
      'maxEmployees': 10,
      'isPopular': false,
      'shortDescription': 'Hasta 10 trabajadores, módulos básicos',
      'modules': modules,
    };

/// Forma del catálogo estático (`SubscriptionPlanConfig.fallbackAll`): `id` ya
/// es la clave de negocio y no viene `planId` ⇒ plan no editable.
Map<String, dynamic> get _staticPlan => {
      'id': 'starter',
      'name': 'Starter',
      'price': 'Gratis',
      'period': 'Para siempre',
      'maxEmployees': 10,
      'isPopular': false,
      'shortDescription': 'Hasta 10 trabajadores, módulos básicos',
    };

ResponseBody _json(Object? data, [int status = 200]) => ResponseBody.fromString(
      jsonEncode(data),
      status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

/// `SecureStorage` de prueba: en `flutter test` el canal del plugin nunca
/// responde dentro del reloj falso, así que el token se sirve desde memoria
/// (si no, el interceptor de `DioClient` deja el request colgado).
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

/// API falsa con los 3 endpoints que consume la página.
class _FakeApi {
  _FakeApi({required this.plans}) {
    adapter = _FakeAdapter(_handle);
  }

  final List<Map<String, dynamic>> plans;
  late final _FakeAdapter adapter;

  List<RequestOptions> get requests => adapter.requests;
  RequestOptions get lastPut => requests.lastWhere((r) => r.method == 'PUT');

  Future<ResponseBody> _handle(RequestOptions options) async {
    if (options.method == 'PUT') {
      return _json(plans.isEmpty ? <String, dynamic>{} : plans.first);
    }
    if (options.path == 'companies/subscription-plans') return _json(plans);
    if (options.path == 'companies/all') return _json(_companies);
    return _json({'error': 'no encontrado'}, 404);
  }

  static const _companies = [
    {
      'id': 'c1',
      'name': 'Tienda Brizuela',
      'country': 'NIC',
      'maxEmployees': 10,
      'isActive': true,
      'subscriptionPlan': 'starter',
    }
  ];
}

Object? _decoded(Object? data) => data is String ? jsonDecode(data) : data;

Future<_FakeApi> _pumpPage(
  WidgetTester tester,
  List<Map<String, dynamic>> plans, {
  Size size = const Size(1024, 1000),
}) async {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  final api = _FakeApi(plans: plans);
  // El adapter falso se engancha en el plumbing real (DioClient + Dio).
  final client = DioClient(_TestStorage(), adapter: api.adapter);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(client)],
      child: const MaterialApp(home: SubscriptionPlansPage()),
    ),
  );
  await tester.pumpAndSettle();
  return api;
}

Future<void> _openEditor(WidgetTester tester) async {
  final button = find.widgetWithText(ZButton, 'Editar módulos');
  await tester.ensureVisible(button);
  await tester.tap(button);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('edita los módulos del plan y hace el PUT con el payload',
      (tester) async {
    final api = await _pumpPage(tester, [_dbPlan()]);

    // La tarjeta muestra los módulos actuales del plan.
    expect(find.text('MÓDULOS DEL PLAN'), findsOneWidget);
    expect(find.text('Ventas'), findsOneWidget);
    expect(find.widgetWithText(ZButton, 'Editar módulos'), findsOneWidget);

    await _openEditor(tester);

    expect(find.text('Módulos de Starter'), findsOneWidget);
    expect(find.byType(SwitchListTile), findsNWidgets(11));

    // Activa "Compras" (índice 2 del catálogo; venía apagado en Starter).
    final purchases = find.byType(SwitchListTile).at(2);
    await tester.ensureVisible(purchases);
    await tester.tap(purchases);
    await tester.pump();

    await tester.tap(find.widgetWithText(ZButton, 'Guardar'));
    await tester.pumpAndSettle();

    final put = api.lastPut;
    expect(put.method, 'PUT');
    expect(put.path, 'subscription-plans/$_planGuid/modules');
    expect(put.headers['Authorization'], 'Bearer test-token');
    expect(_decoded(put.data), {
      'modules': ['Sales', 'Inventory', 'Purchases', 'Hr', 'Administration'],
    });
  });

  testWidgets('avisa y permite guardar un plan sin ningún módulo',
      (tester) async {
    final api = await _pumpPage(tester, [_dbPlan()]);
    await _openEditor(tester);

    await tester.tap(find.text('Ninguno'));
    await tester.pumpAndSettle();

    expect(find.textContaining('no verán NINGÚN módulo del ERP'), findsOneWidget);
    final save = find.widgetWithText(ZButton, 'Guardar sin módulos');
    expect(save, findsOneWidget);

    await tester.tap(save);
    await tester.pumpAndSettle();

    expect(api.lastPut.path, 'subscription-plans/$_planGuid/modules');
    expect(_decoded(api.lastPut.data), {'modules': <dynamic>[]});
  });

  testWidgets('el catálogo estático (sin GUID) no ofrece editar módulos',
      (tester) async {
    final api = await _pumpPage(tester, [_staticPlan]);

    // Los módulos se listan (espejo local) pero sin botón de edición.
    expect(find.text('MÓDULOS DEL PLAN'), findsOneWidget);
    expect(find.text('Ventas'), findsOneWidget);
    expect(find.widgetWithText(ZButton, 'Editar módulos'), findsNothing);
    expect(api.requests.where((r) => r.method == 'PUT'), isEmpty);
  });

  testWidgets('a 360 px las tarjetas de plan no desbordan', (tester) async {
    await _pumpPage(
      tester,
      [_dbPlan()],
      size: const Size(360, 900),
    );

    expect(find.text('MÓDULOS DEL PLAN'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
