import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/core/providers/company_branch_provider.dart';
import 'package:zorvian/features/employees/pages/employee_detail_page.dart';
import 'package:zorvian/features/employees/pages/employee_form_page.dart';
import 'package:zorvian/features/employees/pages/employee_list_page.dart';
import 'package:zorvian/shared/ds/ds.dart';

import 'helpers/fake_api.dart';

/// Estados de carga de la UI de empleado.
///
/// El listado pintaba el `loadingWidget` por defecto de `ZAsyncRenderer`
/// (una única barra gris de 100 px sobre un Scaffold casi blanco) y el
/// detalle un `Scaffold` vacío con un spinner: la pantalla se quedaba
/// "gris o en blanco" mientras respondía la API. Aquí se congela la carga
/// para assertear que se pinta el esqueleto con la forma de la página real.
///
/// La API tarda 1 s A PROPÓSITO: con una respuesta síncrona el estado de
/// carga desaparece dentro del primer `pump` y el test pasaría sin probar
/// nada (mismo criterio que `helpers/fake_api.dart` para el token de auth).
void main() {
  FakeApi slowApi(Object? body) => FakeApi((options) async {
        await Future<void>.delayed(const Duration(seconds: 1));
        return jsonResponse(body);
      });

  testWidgets('listado de empleado pinta filas de esqueleto mientras carga', (tester) async {
    final api = slowApi(const {
      'items': <dynamic>[],
      'total': 0,
      'page': 1,
      'pageSize': 20,
    });

    await tester.pumpWidget(ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(api.client)],
      child: const MaterialApp(home: EmployeeListPage()),
    ));
    await tester.pump();

    // Varias barras = forma de lista; el bug dejaba UNA sola barra de 100 px.
    expect(find.byType(ZSkeleton).evaluate().length, greaterThan(3));
    expect(find.byType(ZEmptyState), findsNothing);

    // Deja llegar la respuesta para no terminar con el ticker del skeleton vivo.
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    expect(find.byType(ZEmptyState), findsOneWidget);
  });

  testWidgets('detalle de empleado pinta esqueleto, no pantalla en blanco', (tester) async {
    final api = slowApi(const {
      'id': 'e1',
      'firstName': 'Juan',
      'lastName': 'Pérez',
      'position': 'Contador',
      'status': 'active',
    });

    await tester.pumpWidget(ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(api.client)],
      child: const MaterialApp(home: EmployeeDetailPage(employeeId: 'e1')),
    ));
    await tester.pump();

    expect(find.byType(ZSkeleton).evaluate().length, greaterThan(0));
    // El estado anterior era un Scaffold vacío con solo el spinner.
    expect(find.byType(CircularProgressIndicator), findsNothing);

    // `_load` hace DOS peticiones encadenadas (empleado + historial de
    // contratos), cada una con su retardo de 1 s.
    await tester.pump(const Duration(seconds: 1));
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    expect(find.text('Juan Pérez'), findsWidgets);
  });

  testWidgets('formulario de empleado en edición carga con esqueleto, no vacío', (tester) async {
    final api = slowApi(const {
      'id': 'e1',
      'firstName': 'Juan',
      'lastName': 'Pérez',
      'position': 'Contador',
      'status': 'active',
    });

    await tester.pumpWidget(ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(api.client)],
      child: const MaterialApp(home: EmployeeFormPage(employeeId: 'e1')),
    ));
    await tester.pump();

    // El stepper da contexto y el cuerpo es esqueleto: antes se pintaba el
    // form completamente vacío, con el botón "Continuar" listo para
    // validarlo contra campos que aún no cargaban.
    expect(find.text('Datos Personales'), findsOneWidget);
    expect(find.byType(ZSkeleton).evaluate().length, greaterThan(0));
    expect(find.text('Continuar'), findsNothing);

    // El form dispara dos peticiones en paralelo (empleado + departamentos).
    await tester.pump(const Duration(seconds: 1));
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();

    expect(find.byType(ZSkeleton), findsNothing);
    expect(find.text('Continuar'), findsOneWidget);
    expect(find.text('Juan'), findsOneWidget);
  });

  Map<String, Object?> row(String id, String name) => {
        'id': id,
        'employeeCode': 'EMP-$id',
        'fullName': name,
        'email': '$id@example.com',
        'departmentName': 'Dirección',
        'position': 'Analista',
        'status': 'active',
        'hireDate': '2026-01-01',
      };

  Map<String, Object?> page(List<Map<String, Object?>> items) => {
        'items': items,
        'total': items.length,
        'page': 1,
        'pageSize': 20,
      };

  testWidgets('buscar conserva las filas en pantalla mientras carga', (tester) async {
    final api = FakeApi((options) async {
      if (options.queryParameters['search'] != null) {
        await Future<void>.delayed(const Duration(seconds: 1));
        return jsonResponse(page([row('e9', 'Zoe')]));
      }
      await Future<void>.delayed(const Duration(milliseconds: 50));
      return jsonResponse(page([row('e1', 'Ana'), row('e2', 'Luis')]));
    });

    await tester.pumpWidget(ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(api.client)],
      child: const MaterialApp(home: EmployeeListPage()),
    ));
    await tester.pumpAndSettle();
    expect(find.text('Ana'), findsOneWidget);
    expect(find.text('Luis'), findsOneWidget);

    await tester.enterText(find.byType(TextField), 'zo');
    await tester.pump();

    // La búsqueda sigue en vuelo: las filas anteriores permanecen y NO se
    // vuelve al esqueleto (ese era el bug).
    expect(find.text('Ana'), findsOneWidget);
    expect(find.text('Luis'), findsOneWidget);
    expect(find.byType(ZSkeleton), findsNothing);

    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    expect(find.text('Zoe'), findsOneWidget);
    expect(find.text('Ana'), findsNothing);
  });

  testWidgets('cambiar de sucursal refetchea y cambia las filas', (tester) async {
    var calls = 0;
    final api = FakeApi((options) async {
      if (options.path.contains('employees')) {
        calls++;
        await Future<void>.delayed(const Duration(milliseconds: 50));
        return jsonResponse(page([row('e$calls', calls == 1 ? 'Ana' : 'Luis')]));
      }
      await Future<void>.delayed(const Duration(milliseconds: 50));
      return jsonResponse(const []);
    });

    await tester.pumpWidget(ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(api.client)],
      child: const MaterialApp(home: EmployeeListPage()),
    ));
    await tester.pumpAndSettle();
    expect(find.text('Ana'), findsOneWidget);

    final container = ProviderScope.containerOf(
      tester.element(find.byType(EmployeeListPage)),
    );
    container.read(companyBranchProvider.notifier).selectBranch('b2', 'Sucursal 2');
    await tester.pumpAndSettle();

    expect(calls, 2, reason: 'el cambio de sucursal debe volver a pedir los datos');
    expect(find.text('Luis'), findsOneWidget);
    expect(find.text('Ana'), findsNothing);
  });
}
