import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/admin/pages/roles_permissions_page.dart';
import 'package:zorvian/shared/ds/ds.dart';

/// Rol del sistema (no editable más allá de sus permisos).
Map<String, dynamic> get _systemRole => {
      'id': 'r-system',
      'name': 'CompanyAdmin',
      'displayName': 'Admin',
      'description': null,
      'isSystem': true,
      'permissions': ['sale.read'],
    };

/// Rol personalizado (renombrable / eliminable).
Map<String, dynamic> get _customRole => {
      'id': 'r-custom',
      'name': 'Custom',
      'displayName': 'Auditor',
      'description': 'Solo lectura',
      'isSystem': false,
      'permissions': ['sale.read'],
    };

const _catalog = [
  {
    'code': 'sale.read',
    'label': 'Ver ventas',
    'group': 'Ventas',
    'isSuperAdminOnly': false,
  },
  {
    'code': 'sale.write',
    'label': 'Gestionar ventas',
    'group': 'Ventas',
    'isSuperAdminOnly': false,
  },
];

ResponseBody _json(Object? data, [int status = 200]) => ResponseBody.fromString(
      jsonEncode(data),
      status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

/// `SecureStorage` de prueba: el canal del plugin nunca responde dentro del
/// reloj falso de `flutter test`.
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
  _FakeApi({required this.roles}) {
    adapter = _FakeAdapter(_handle);
  }

  final List<Map<String, dynamic>> roles;
  late final _FakeAdapter adapter;

  List<RequestOptions> get requests => adapter.requests;
  RequestOptions get lastPost =>
      requests.lastWhere((r) => r.method == 'POST');
  RequestOptions get lastPut => requests.lastWhere((r) => r.method == 'PUT');
  RequestOptions get lastDelete =>
      requests.lastWhere((r) => r.method == 'DELETE');

  Future<ResponseBody> _handle(RequestOptions options) async {
    if (options.method == 'GET' && options.path == 'roles') {
      return _json(roles);
    }
    if (options.method == 'GET' && options.path == 'roles/permissions-catalog') {
      return _json(_catalog);
    }
    if (options.method == 'POST' && options.path == 'roles') {
      return _json(_customRole, 201);
    }
    if (options.method == 'PUT') return _json({'message': 'ok'});
    if (options.method == 'DELETE') return ResponseBody.fromString('', 204);
    return _json({'error': 'no encontrado'}, 404);
  }
}

Object? _decoded(Object? data) => data is String ? jsonDecode(data) : data;

Future<_FakeApi> _pumpPage(
  WidgetTester tester,
  List<Map<String, dynamic>> roles, {
  Size size = const Size(900, 1600),
}) async {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  final api = _FakeApi(roles: roles);
  final client = DioClient(_TestStorage(), adapter: api.adapter);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(client)],
      child: const MaterialApp(home: RolesPermissionsPage()),
    ),
  );
  await tester.pumpAndSettle();
  return api;
}

/// Botón del diálogo abierto (no confundir con los "Guardar" de las tarjetas).
Finder _dialogButton(String label) => find.descendant(
      of: find.byType(ZResponsiveDialog),
      matching: find.widgetWithText(ZButton, label),
    );

void main() {
  testWidgets('crea un rol personalizado y hace el POST con el payload',
      (tester) async {
    final api = await _pumpPage(tester, [_systemRole, _customRole]);

    await tester.tap(find.widgetWithText(ZButton, 'Nuevo rol'));
    await tester.pumpAndSettle();

    expect(find.text('Nuevo rol'), findsWidgets);
    await tester.enterText(find.byType(TextField).at(0), 'Auditor de Ventas');
    await tester.tap(_dialogButton('Guardar'));
    await tester.pumpAndSettle();

    final post = api.lastPost;
    expect(post.path, 'roles');
    expect(post.headers['Authorization'], 'Bearer test-token');
    expect(_decoded(post.data), {
      'displayName': 'Auditor de Ventas',
      'description': null,
      'permissions': <dynamic>[],
    });

    // Deja expirar el SnackBar antes del teardown.
    await tester.pump(const Duration(seconds: 5));
  });

  testWidgets('renombra un rol personalizado con PUT', (tester) async {
    final api = await _pumpPage(tester, [_customRole]);

    // Solo el rol personalizado ofrece el menú de acciones.
    await tester.tap(find.byIcon(Icons.more_vert));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Renombrar / editar'));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField).at(0), 'Auditor Senior');
    await tester.tap(_dialogButton('Guardar'));
    await tester.pumpAndSettle();

    final put = api.lastPut;
    expect(put.path, 'roles/r-custom');
    expect(_decoded(put.data), {
      'displayName': 'Auditor Senior',
      'description': 'Solo lectura',
    });

    await tester.pump(const Duration(seconds: 5));
  });

  testWidgets('elimina un rol personalizado tras confirmar', (tester) async {
    final api = await _pumpPage(tester, [_customRole]);

    await tester.tap(find.byIcon(Icons.more_vert));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Eliminar rol'));
    await tester.pumpAndSettle();

    expect(find.textContaining('¿Eliminar el rol "Auditor"?'), findsOneWidget);
    await tester.tap(find.descendant(
      of: find.byType(ZResponsiveDialog),
      matching: find.text('Eliminar'),
    ));
    await tester.pumpAndSettle();

    expect(api.lastDelete.path, 'roles/r-custom');

    await tester.pump(const Duration(seconds: 5));
  });

  testWidgets('los roles del sistema no se renombran ni se eliminan',
      (tester) async {
    final api = await _pumpPage(tester, [_systemRole]);

    expect(find.byIcon(Icons.more_vert), findsNothing);
    expect(find.widgetWithText(ZButton, 'Guardar'), findsOneWidget);
    expect(
      api.requests.where((r) => r.method == 'PUT' || r.method == 'DELETE'),
      isEmpty,
    );
  });

  testWidgets('a 360 px la matriz de roles se apila sin desbordar',
      (tester) async {
    await _pumpPage(
      tester,
      [_systemRole, _customRole],
      size: const Size(360, 900),
    );

    expect(find.byType(RolesPermissionsPage), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
