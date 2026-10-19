import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/fiscal/pages/country_tax_config_page.dart';

/// La pantalla de configuración fiscal es ADAPTATIVA por rol:
///  - SuperAdmin → catálogo GLOBAL (`GET country-tax-configs`).
///  - Resto → configuración de SU país (`GET country-tax-configs/my-country`),
///    sin poder tocar el catálogo del resto de las compañías.

const _irTable = '[{"min":0,"max":100000,"rate":0}]';

Map<String, dynamic> _config() => {
      'id': 'cfg-1',
      'countryCode': 'NIC',
      'countryName': 'Nicaragua',
      'currency': 'NIO',
      'inssEmployeeRate': 0.07,
      'inssEmployerMax': 20000,
      'inssEmployerRate': 0.215,
      'otherEmployerRate': 0.02,
      'otherEmployerName': 'INATEC',
      'inssIntegralEmployeeRate': 0.045,
      'inssIntegralEmployerRate': 0.225,
      'inssIntegralEmployerRateSmall': 0.215,
      'inssIvmEmployeeRate': 0.07,
      'inssIvmEmployerRate': 0.19,
      'inssIvmEmployerRateSmall': 0.18,
      'inssSmallEmployerThreshold': 50,
      'irExemptAmount': 100000,
      'irTableJson': _irTable,
      'vacationDaysPerYear': 15,
      'christmasBonusPercentage': 0.0833,
      'indemnityDaysPerYear': 30,
      'maxIndemnityYears': 5,
      'indemnityTiersJson': '[]',
      'maxIndemnityDays': 150,
      'hasThirteenthMonth': true,
      'hasFourteenthMonth': false,
      'aguinaldoPeriodStartMonth': 12,
      'aguinaldoPeriodStartDay': 1,
      'defaultFiscalStartMonth': 1,
      'isActive': true,
    };

class _FixedAuthNotifier extends AuthNotifier {
  final AuthState _fixedState;
  _FixedAuthNotifier(this._fixedState);

  @override
  AuthState build() => _fixedState;
}

AuthState _auth(String role) => AuthState(
      status: AuthStatus.authenticated,
      displayName: 'Test',
      email: 'test@zorvian.com',
      role: role,
      plan: 'enterprise',
    );

class _TestStorage extends SecureStorage {
  @override
  Future<String?> getAccessToken() async => 'test-token';

  @override
  Future<String?> getRefreshToken() async => null;
}

ResponseBody _json(Object? data, [int status = 200]) => ResponseBody.fromString(
      jsonEncode(data),
      status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

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

Future<List<RequestOptions>> _pump(
  WidgetTester tester, {
  required String role,
  bool isCustomized = true,
  bool hasConfig = true,
}) async {
  final adapter = _FakeAdapter((options) async {
    if (options.path == 'country-tax-configs/my-country') {
      return _json({
        'countryCode': 'NIC',
        'countryName': 'Nicaragua',
        'isCustomized': isCustomized,
        'config': hasConfig ? _config() : null,
      });
    }
    if (options.path == 'country-tax-configs') {
      return _json([_config()]);
    }
    return _json({'error': 'unexpected ${options.path}'}, 404);
  });

  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        authProvider.overrideWith(() => _FixedAuthNotifier(_auth(role))),
        dioClientProvider.overrideWithValue(
            DioClient(_TestStorage(), adapter: adapter)),
      ],
      child: const MaterialApp(home: CountryTaxConfigPage()),
    ),
  );
  await tester.pumpAndSettle();
  return adapter.requests;
}

void main() {
  testWidgets('CompanyAdmin ve su país, no el catálogo global', (tester) async {
    final requests = await _pump(tester, role: 'CompanyAdmin');

    expect(requests.any((r) =>
        r.path == 'country-tax-configs/my-country' && r.method == 'GET'), isTrue);
    expect(requests.any((r) => r.path == 'country-tax-configs'), isFalse);

    expect(find.text('Nicaragua'), findsOneWidget);
    expect(find.text('Valores propios'), findsOneWidget);
    expect(find.text('Editar configuración'), findsOneWidget);
    expect(find.text('Restablecer al catálogo global'), findsOneWidget);
  });

  testWidgets('sin ajuste propio no ofrece restablecer (usa el catálogo)',
      (tester) async {
    await _pump(tester, role: 'CompanyAdmin', isCustomized: false);

    expect(find.text('Catálogo global'), findsOneWidget);
    expect(find.text('Restablecer al catálogo global'), findsNothing);
    expect(find.text('Editar configuración'), findsOneWidget);
  });

  testWidgets('compañía sin país configurado muestra el aviso', (tester) async {
    await _pump(tester, role: 'CompanyAdmin', hasConfig: false);

    expect(find.text('Tu compañía no tiene un país configurado'), findsOneWidget);
    expect(find.text('Editar configuración'), findsNothing);
  });

  testWidgets('SuperAdmin conserva el catálogo global', (tester) async {
    final requests = await _pump(tester, role: 'SuperAdmin');

    expect(requests.any((r) => r.path == 'country-tax-configs'), isTrue);
    expect(requests.any((r) => r.path == 'country-tax-configs/my-country'),
        isFalse);

    // Listado con buscador + acción de edición del país.
    expect(find.text('Buscar país o código...'), findsOneWidget);
    expect(find.text('Nicaragua'), findsOneWidget);
  });
}
