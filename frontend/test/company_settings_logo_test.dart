import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/admin/widgets/company_logo_picker.dart';
import 'package:zorvian/features/settings/pages/company_settings_page.dart';

/// `SecureStorage` de prueba: el canal del plugin nunca responde en
/// `flutter test` y el interceptor de `DioClient` dejaría el request colgado.
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

final Uint8List _tinyPng = Uint8List.fromList(const [
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
  0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
  0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
  0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
  0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
  0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82,
]);

ResponseBody _jsonBody(Object? data, [int status = 200]) =>
    ResponseBody.fromString(
      data == null ? '' : jsonEncode(data),
      status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

/// Red falsa de la pantalla de configuración: settings, logo (GET/POST/DELETE).
class _FakeSettingsApi {
  _FakeSettingsApi({this.currentLogoUrl = 'https://signed.test/companies/c1/logo.png'});

  final String? currentLogoUrl;

  late final _FakeAdapter adapter = _FakeAdapter(_handle);

  List<RequestOptions> get requests => adapter.requests;

  List<String> get calls =>
      requests.map((r) => '${r.method} ${r.path}').toList();

  Future<ResponseBody> _handle(RequestOptions options) async {
    final path = options.path;

    if (path == 'companies/settings') {
      return _jsonBody({
        'vacationDaysPerYear': 15,
        'lateToleranceMinutes': 15,
        'workingHoursPerDay': 8,
        'currency': 'NIO',
        'timezone': 'America/Managua',
        'lateFeeDailyRate': 0.001,
        'lateFeePercentage': 0.05,
        'lateFeeGracePeriod': 0,
        'taxEnabled': true,
        'taxRate': 0.15,
        'retentionBase': 'taxable_gross',
      });
    }

    if (options.method == 'GET' && path == 'companies/logo') {
      return _jsonBody({'id': 'c1', 'logoUrl': currentLogoUrl});
    }

    if (path.endsWith('/logo')) {
      if (options.method == 'DELETE') return _jsonBody(null, 204);
      return _jsonBody({'logoUrl': 'https://signed.test/companies/c1/logo.png'});
    }

    return _jsonBody({'error': 'no encontrado'}, 404);
  }
}

void main() {
  Future<void> pumpPage(WidgetTester tester, _FakeSettingsApi api) async {
    tester.view.physicalSize = const Size(1200, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          dioClientProvider.overrideWithValue(
            DioClient(_TestStorage(), adapter: api.adapter),
          ),
          logoPickerProvider.overrideWithValue(
            () async => PickedLogo(bytes: _tinyPng, fileName: 'logo.png'),
          ),
        ],
        child: const MaterialApp(home: CompanySettingsPage()),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('la tarjeta de logo vive en la configuración de la empresa',
      (tester) async {
    // Regresión: una CompanyAdmin no tiene `company.manage`, así que
    // `companies/current` le daba 403 y no tenía dónde editar el logo.
    final api = _FakeSettingsApi();
    await pumpPage(tester, api);

    expect(find.text('Logo de la Empresa'), findsOneWidget);
    expect(find.text('Cambiar logo'), findsOneWidget);
    expect(find.text('Quitar logo'), findsOneWidget);
    expect(api.calls, contains('GET companies/logo'));
  });

  testWidgets('elegir un logo lo sube al instante y refresca la vista',
      (tester) async {
    final api = _FakeSettingsApi();
    await pumpPage(tester, api);

    await tester.tap(find.text('Cambiar logo'));
    await tester.pumpAndSettle();

    expect(api.calls, contains('POST companies/c1/logo'));
    expect(find.text('Logo actualizado'), findsOneWidget);

    // Se vuelve a pedir la URL firmada después de subir.
    final logoGets =
        api.calls.where((c) => c == 'GET companies/logo').length;
    expect(logoGets, greaterThanOrEqualTo(2));
  });

  testWidgets('quitar el logo pide confirmación y llama al DELETE',
      (tester) async {
    final api = _FakeSettingsApi();
    await pumpPage(tester, api);

    await tester.tap(find.text('Quitar logo'));
    await tester.pumpAndSettle();

    expect(find.text('Quitar'), findsOneWidget);
    await tester.tap(find.widgetWithText(FilledButton, 'Quitar'));
    await tester.pumpAndSettle();

    expect(api.calls, contains('DELETE companies/c1/logo'));
    expect(find.text('Logo eliminado'), findsOneWidget);
  });

  testWidgets('sin logo guardado no ofrece quitarlo', (tester) async {
    final api = _FakeSettingsApi(currentLogoUrl: null);
    await pumpPage(tester, api);

    expect(find.text('Subir logo'), findsOneWidget);
    expect(find.text('Quitar logo'), findsNothing);
  });

  testWidgets('un fallo de subida se informa sin romper la pantalla',
      (tester) async {
    final api = _FakeSettingsApi();
    // Adapter que falla solo en el POST del logo.
    final failing = _FakeAdapter((options) async {
      if (options.path.endsWith('/logo') && options.method == 'POST') {
        return _jsonBody({'error': 'Firebase Storage no configurado'}, 502);
      }
      return api.adapter.fetch(options, null, null);
    });

    tester.view.physicalSize = const Size(1200, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          dioClientProvider.overrideWithValue(
            DioClient(_TestStorage(), adapter: failing),
          ),
          logoPickerProvider.overrideWithValue(
            () async => PickedLogo(bytes: _tinyPng, fileName: 'logo.png'),
          ),
        ],
        child: const MaterialApp(home: CompanySettingsPage()),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Cambiar logo'));
    await tester.pumpAndSettle();

    expect(
      find.textContaining('No se pudo guardar el logo'),
      findsOneWidget,
    );
    expect(find.textContaining('Firebase Storage no configurado'),
        findsOneWidget);
  });
}
