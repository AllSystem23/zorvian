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
import 'package:zorvian/shared/ds/ds.dart';

// La pantalla de configuración fiscal es la ÚNICA vía para editar el catálogo
// global de tasas. El bug que motivó estos tests: el formulario no cargaba los
// valores existentes y, al guardar, los reseteaba (desaparecían los regímenes
// INSS, la tabla de IR, los escalones de indemnización y el período de aguinaldo).

const _irTable =
    '[{"min":0,"max":100000,"rate":0},{"min":100000,"max":200000,"rate":0.15}]';
const _tiers =
    '[{"upToYears":3,"daysPerYear":30},{"upToYears":6,"daysPerYear":20}]';

Map<String, dynamic> _config() => {
      'id': 'cfg-1',
      'countryCode': 'NIC',
      'countryName': 'Nicaragua',
      'currency': 'NIO',
      'inssEmployeeRate': 0.07,
      'inssEmployeeMax': 20000,
      'inssEmployerRate': 0.215,
      'inssEmployerMax': 0,
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
      'indemnityTiersJson': _tiers,
      'maxIndemnityDays': 150,
      'hasThirteenthMonth': true,
      'hasFourteenthMonth': false,
      'aguinaldoPeriodStartMonth': 12,
      'aguinaldoPeriodStartDay': 1,
      'defaultFiscalStartMonth': 1,
      'isActive': true,
    };

ResponseBody _json(Object? data, [int status = 200]) => ResponseBody.fromString(
      jsonEncode(data),
      status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

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
  late final _FakeAdapter adapter =
      _FakeAdapter((options) async => _json(_config()));

  List<RequestOptions> get requests => adapter.requests;

  RequestOptions get lastWrite =>
      requests.lastWhere((r) => r.method == 'PUT' || r.method == 'POST');
}

/// Abre el formulario igual que producción: `showModalBottomSheet` con el form
/// dentro (el form hace `Navigator.pop` al guardar).
Future<_FakeApi> _pumpForm(
  WidgetTester tester, {
  Map<String, dynamic>? existing,
  List<String>? saved,
  bool companyMode = false,
}) async {
  // Viewport alto: el formulario es un ListView (los hijos fuera de pantalla no se
  // construyen) y casi todo el contenido debe estar visible para poder inspeccionarlo.
  tester.view.physicalSize = const Size(1400, 3200);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  final api = _FakeApi();
  final client = DioClient(_TestStorage(), adapter: api.adapter);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(client)],
      child: MaterialApp(
        // Sin Scaffold externo: el SnackBar se mostraría en los dos Scaffolds
        // registrados en el ScaffoldMessenger (duplicado en los finders).
        home: Builder(
          builder: (ctx) => Center(
            child: TextButton(
              onPressed: () => showModalBottomSheet(
                context: ctx,
                isScrollControlled: true,
                useSafeArea: true,
                builder: (_) => CountryTaxConfigForm(
                  existing: existing == null
                      ? null
                      : CountryTaxConfigItem.fromJson(existing),
                  companyMode: companyMode,
                  onSaved: () => saved?.add('saved'),
                ),
              ),
              child: const Text('abrir'),
            ),
          ),
        ),
      ),
    ),
  );

  await tester.tap(find.text('abrir'));
  await tester.pumpAndSettle();
  return api;
}

/// `ZTextField` pinta la etiqueta en MAYÚSCULAS.
Finder _field(String label) =>
    find.widgetWithText(ZTextField, label.toUpperCase());

Future<void> _save(WidgetTester tester) async {
  await tester.tap(find.widgetWithText(TextButton, 'Guardar'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('editar sin cambios preserva TODOS los campos (sin pérdida de datos)',
      (tester) async {
    final saved = <String>[];
    final api = await _pumpForm(tester, existing: _config(), saved: saved);

    await _save(tester);

    final put = api.lastWrite;
    expect(put.method, 'PUT');
    expect(put.path, 'country-tax-configs/cfg-1');

    final body = put.data as Map<String, dynamic>;
    expect(body['countryCode'], 'NIC');
    expect(body['countryName'], 'Nicaragua');
    expect(body['currency'], 'NIO');
    expect(body['inssEmployeeRate'], 0.07);
    expect(body['inssEmployeeMax'], 20000);
    expect(body['inssEmployerRate'], 0.215);
    expect(body['otherEmployerRate'], 0.02);
    expect(body['otherEmployerName'], 'INATEC');
    expect(body['inssIntegralEmployeeRate'], 0.045);
    expect(body['inssIntegralEmployerRate'], 0.225);
    expect(body['inssIntegralEmployerRateSmall'], 0.215);
    expect(body['inssIvmEmployeeRate'], 0.07);
    expect(body['inssIvmEmployerRate'], 0.19);
    expect(body['inssIvmEmployerRateSmall'], 0.18);
    expect(body['inssSmallEmployerThreshold'], 50);
    expect(body['irExemptAmount'], 100000);
    expect(body['vacationDaysPerYear'], 15);
    expect(body['christmasBonusPercentage'], 0.0833);
    expect(body['indemnityDaysPerYear'], 30);
    expect(body['maxIndemnityYears'], 5);
    expect(body['maxIndemnityDays'], 150);
    expect(body['hasThirteenthMonth'], isTrue);
    expect(body['hasFourteenthMonth'], isFalse);
    expect(body['aguinaldoPeriodStartMonth'], 12);
    expect(body['aguinaldoPeriodStartDay'], 1);
    expect(body['defaultFiscalStartMonth'], 1);
    expect(body['isActive'], isTrue);

    // Las dos tablas JSON sobreviven el round-trip (era el bug: se enviaban vacías).
    expect(jsonDecode(body['irTableJson'] as String), jsonDecode(_irTable));
    expect(
        jsonDecode(body['indemnityTiersJson'] as String), jsonDecode(_tiers));

    expect(saved, hasLength(1));
  });

  testWidgets('las tablas de IR e indemnización se cargan como filas editables',
      (tester) async {
    await _pumpForm(tester, existing: _config());

    // 2 tramos de IR cargados del backend.
    expect(_field('Desde'), findsNWidgets(2));
    expect(_field('Hasta'), findsNWidgets(2));

    // Los valores llegaron a los campos (no arrancan en '0' como antes).
    final desde = tester.widgetList<ZTextField>(_field('Desde')).toList();
    expect(desde[0].controller!.text, '0');
    expect(desde[1].controller!.text, '100000');

    // Los escalones de indemnización están más abajo en el formulario.
    await tester.drag(find.byType(ListView).first, const Offset(0, -1200));
    await tester.pumpAndSettle();

    expect(_field('Hasta (años)'), findsNWidgets(2));
    expect(_field('Días por año'), findsNWidgets(2));
    final hastaAnios =
        tester.widgetList<ZTextField>(_field('Hasta (años)')).toList();
    expect(hastaAnios[0].controller!.text, '3');
    expect(hastaAnios[1].controller!.text, '6');
  });

  testWidgets('agregar un tramo de IR lo serializa junto a los existentes',
      (tester) async {
    final api = await _pumpForm(tester, existing: _config());

    final addBracket = find.text('Agregar tramo');
    await tester.ensureVisible(addBracket);
    await tester.pumpAndSettle();
    await tester.tap(addBracket);
    await tester.pumpAndSettle();

    expect(_field('Desde'), findsNWidgets(3));

    // La fila nueva es la última: se completa desde/hasta/tasa.
    await tester.enterText(_field('Desde').last, '200000');
    await tester.enterText(_field('Hasta').last, '300000');
    await tester.enterText(_field('Tasa').last, '0.20');
    await tester.pump();

    await _save(tester);

    final body = api.lastWrite.data as Map<String, dynamic>;
    final ir = jsonDecode(body['irTableJson'] as String) as List;
    expect(ir, hasLength(3));
    expect(ir[0]['rate'], 0);
    expect(ir[1]['rate'], 0.15);
    expect(ir[2], {'min': 200000, 'max': 300000, 'rate': 0.2});
  });

  testWidgets('un tramo incompleto bloquea el guardado (no se pierde la tabla)',
      (tester) async {
    final api = await _pumpForm(tester, existing: _config());

    final addBracket = find.text('Agregar tramo');
    await tester.ensureVisible(addBracket);
    await tester.pumpAndSettle();
    await tester.tap(addBracket);
    await tester.pumpAndSettle();

    // Se deja vacío y se intenta guardar.
    await tester.tap(find.widgetWithText(TextButton, 'Guardar'));
    await tester.pump();
    await tester.pump();

    expect(find.textContaining('Revise la tabla de IR'), findsOneWidget);
    expect(api.requests.where((r) => r.method == 'PUT'), isEmpty);

    await tester.pump(const Duration(seconds: 5)); // limpia el SnackBar
  });

  testWidgets('un nombre de otro aporte vacío se envía para limpiarlo',
      (tester) async {
    final api = await _pumpForm(tester, existing: _config());

    final name = _field('Nombre del otro aporte');
    await tester.ensureVisible(name);
    await tester.enterText(name, '');
    await tester.pump();

    await _save(tester);

    final body = api.lastWrite.data as Map<String, dynamic>;
    // Cadena vacía: el backend la normaliza a null y la liquidación oculta la fila.
    expect(body['otherEmployerName'], '');
  });

  testWidgets('crear un país arranca con los valores por defecto y hace POST',
      (tester) async {
    final api = await _pumpForm(tester);

    await tester.enterText(_field('Código ISO (ej: NIC)'), 'hnd');
    await tester.enterText(_field('Nombre del País'), 'Honduras');
    await tester.enterText(_field('Moneda (ej: NIO)'), 'hnl');
    await tester.pump();

    await _save(tester);

    final post = api.lastWrite;
    expect(post.method, 'POST');
    expect(post.path, 'country-tax-configs');

    final body = post.data as Map<String, dynamic>;
    expect(body['countryCode'], 'HND'); // se normaliza a mayúsculas
    expect(body['currency'], 'HNL');
    expect(body['vacationDaysPerYear'], 15);
    expect(body['inssSmallEmployerThreshold'], 50);
    expect(body['aguinaldoPeriodStartMonth'], 12);
    expect(body['indemnityTiersJson'], '[]'); // sin escalones configurados
    expect(jsonDecode(body['irTableJson'] as String), isEmpty);
  });

  // ── Modo compañía: ajuste sobre el catálogo global ──

  testWidgets('modo compañía guarda en my-country y conserva el país',
      (tester) async {
    final api =
        await _pumpForm(tester, existing: _config(), companyMode: true);

    // La identidad del país la define el catálogo global: aquí no se edita.
    expect(_field('Código ISO (ej: NIC)'), findsNothing);
    expect(_field('Nombre del País'), findsNothing);
    expect(find.textContaining('Estos valores aplican SÓLO a tu compañía'),
        findsOneWidget);

    await _save(tester);

    final put = api.lastWrite;
    expect(put.method, 'PUT');
    expect(put.path, 'country-tax-configs/my-country');

    final body = put.data as Map<String, dynamic>;
    expect(body['countryCode'], 'NIC'); // se conserva el del catálogo
    expect(body['countryName'], 'Nicaragua');
    expect(body['inssEmployeeRate'], 0.07);
    expect(body['irTableJson'], isNotNull);
    // Un ajuste inactivo no tendría efecto: desde aquí siempre va activo.
    expect(body['isActive'], isTrue);
  });
}
