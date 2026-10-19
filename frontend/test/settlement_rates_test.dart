import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/payroll/pages/settlement_form_page.dart';
import 'package:zorvian/shared/ds/ds.dart';

const _employeeId = 'e1';

/// Registro de liquidación tal como lo devuelve `POST termination/calculate`
/// (entidad serializada). Las claves `*RateDisplay` son las tasas EFECTIVAS que
/// el backend usó y que el PDF imprime.
Map<String, dynamic> _record({
  double inssEmployee = 0.05,
  double inssEmployer = 0.25,
  double otherEmployer = 0.03,
  String otherEmployerName = 'RAP',
}) =>
    {
      'id': 'r1',
      'employeeId': _employeeId,
      'reason': 'VoluntaryResignation',
      'terminationDate': '2026-06-01',
      'hireDate': '2023-06-01',
      'daysWorked': 1095,
      'monthsWorked': 36,
      'yearsWorked': 3,
      'monthlySalary': 3000,
      'dailySalary': 100,
      'aguinaldoPay': 1000,
      'vacationDaysAccrued': 90,
      'vacationDaysTaken': 0,
      'vacationDaysToPay': 90,
      'vacationPay': 9000,
      'severanceDays': 90,
      'severancePay': 9000,
      'pendingSalaryDays': 1,
      'pendingSalaryPay': 100,
      'overtimeHours': 0,
      'overtimePay': 0,
      'inssLaboralAmount': 455,
      'irSalaryAmount': 0,
      'irTotalAmount': 0,
      'inssPatronalAmount': 2275,
      'inatecAmount': 273,
      'grossSettlement': 19100,
      'totalDeductions': 455,
      'netSettlement': 18645,
      'countryCode': 'NIC',
      'currency': 'NIO',
      'countryName': 'Nicaragua',
      'inssEmployeeRateDisplay': inssEmployee,
      'inssEmployerRateDisplay': inssEmployer,
      'otherEmployerRateDisplay': otherEmployer,
      'otherEmployerName': otherEmployerName,
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
  _FakeApi({this.record});

  final Map<String, dynamic>? record;
  late final _FakeAdapter adapter = _FakeAdapter(_handle);

  List<RequestOptions> get requests => adapter.requests;

  RequestOptions get lastCalculate => requests.lastWhere(
        (r) => r.method == 'POST' && r.path == 'termination/calculate',
      );

  Future<ResponseBody> _handle(RequestOptions options) async {
    if (options.path == 'employees/$_employeeId') {
      return _json({
        'id': _employeeId,
        'firstName': 'Ana',
        'lastName': 'López',
        'position': 'Contadora',
        'hireDate': '2023-06-01T00:00:00',
        'salary': 3000,
        'countryCode': 'NIC',
      });
    }
    if (options.path == 'termination/calculate' && options.method == 'POST') {
      return _json(record ?? _record());
    }
    return _json({'message': 'no encontrado'}, 404);
  }
}

Future<_FakeApi> _pumpPage(WidgetTester tester, {Map<String, dynamic>? record}) async {
  tester.view.physicalSize = const Size(1200, 1400);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  final api = _FakeApi(record: record);
  final client = DioClient(_TestStorage(), adapter: api.adapter);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(client)],
      child: const MaterialApp(
        home: SettlementFormPage(employeeId: _employeeId, companyId: 'c1'),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return api;
}

/// `ZTextField` pinta la etiqueta en MAYÚSCULAS, así que el finder la normaliza.
Finder _rateField(String label) =>
    find.widgetWithText(ZTextField, label.toUpperCase());

Future<void> _calculate(WidgetTester tester) async {
  final button = find.widgetWithText(ZButton, 'Calcular Liquidación');
  await tester.ensureVisible(button);
  await tester.tap(button);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('las tasas del finiquito son editables y viajan al cálculo',
      (tester) async {
    final api = await _pumpPage(tester);

    // La tarjeta de tasas está presente y arranca VACÍA (= configuración del país).
    expect(find.text('Tasas de la Liquidación (Editables)'), findsOneWidget);
    final inssEmployee = _rateField('INSS Trabajador (%)');
    await tester.ensureVisible(inssEmployee);
    expect(tester.widget<ZTextField>(inssEmployee).controller!.text, isEmpty);

    // El usuario fija el INSS del trabajador en 5 % antes de calcular.
    await tester.enterText(inssEmployee, '5');
    await tester.pump();

    await _calculate(tester);

    final post = api.lastCalculate;
    expect(post.queryParameters['employeeId'], _employeeId);
    // 5 % se envía como fracción (el backend valida 0..1).
    expect(post.queryParameters['inssEmployeeRate'], closeTo(0.05, 1e-9));
    // Los campos vacíos NO se envían: el backend usa la configuración del país.
    expect(post.queryParameters.containsKey('inssEmployerRate'), isFalse);
    expect(post.queryParameters.containsKey('otherEmployerRate'), isFalse);
    expect(post.queryParameters.containsKey('otherEmployerName'), isFalse);
  });

  testWidgets('tras calcular, las tasas efectivas se precargan y no se pisan',
      (tester) async {
    // El backend responde con las tasas efectivas del país.
    final api = await _pumpPage(
      tester,
      record: _record(inssEmployee: 0.07, inssEmployer: 0.215, otherEmployer: 0.02, otherEmployerName: 'INATEC'),
    );

    await _calculate(tester);

    // Se precargan en porcentaje con el nombre del aporte.
    expect(tester.widget<ZTextField>(_rateField('INSS Trabajador (%)')).controller!.text, '7.00');
    expect(tester.widget<ZTextField>(_rateField('INSS Patronal (%)')).controller!.text, '21.50');
    expect(tester.widget<ZTextField>(_rateField('Otro Aporte Patronal (%)')).controller!.text, '2.00');
    expect(
      tester.widget<ZTextField>(_rateField('Nombre del otro aporte (INATEC, RAP)')).controller!.text,
      'INATEC',
    );

    // Un segundo cálculo con esos valores mantiene el finiquito estable.
    await _calculate(tester);

    final post = api.lastCalculate;
    expect(post.queryParameters['inssEmployeeRate'], closeTo(0.07, 1e-9));
    expect(post.queryParameters['inssEmployerRate'], closeTo(0.215, 1e-9));
    expect(post.queryParameters['otherEmployerRate'], closeTo(0.02, 1e-9));
    expect(post.queryParameters['otherEmployerName'], 'INATEC');
  });

  testWidgets('un aporte ajustado a 0 % y sin nombre vuelve al respaldo',
      (tester) async {
    final api = await _pumpPage(tester, record: _record());

    await _calculate(tester);

    // El usuario borra el otro aporte (país sin INATEC, p. ej. Costa Rica).
    await tester.enterText(_rateField('Otro Aporte Patronal (%)'), '0');
    await tester.enterText(_rateField('Nombre del otro aporte (INATEC, RAP)'), '');
    await tester.pump();

    await _calculate(tester);

    final post = api.lastCalculate;
    expect(post.queryParameters['otherEmployerRate'], 0);
    // Nombre vacío = no viaja; el backend lo resuelve del país.
    expect(post.queryParameters.containsKey('otherEmployerName'), isFalse);
  });

  testWidgets('Restablecer limpia las tasas y vuelve a la configuración del país',
      (tester) async {
    final api = await _pumpPage(tester, record: _record());

    await _calculate(tester);
    expect(tester.widget<ZTextField>(_rateField('INSS Trabajador (%)')).controller!.text, '5.00');

    final reset = find.widgetWithText(TextButton, 'Restablecer');
    await tester.ensureVisible(reset);
    await tester.tap(reset);
    await tester.pumpAndSettle();

    expect(tester.widget<ZTextField>(_rateField('INSS Trabajador (%)')).controller!.text, isEmpty);

    await _calculate(tester);

    final post = api.lastCalculate;
    expect(post.queryParameters.containsKey('inssEmployeeRate'), isFalse);
    expect(post.queryParameters.containsKey('inssEmployerRate'), isFalse);
    expect(post.queryParameters.containsKey('otherEmployerRate'), isFalse);
  });
}
