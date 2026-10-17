import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:zorvian/core/providers/company_branch_provider.dart';
import 'package:zorvian/core/widgets/header/global_header.dart';

import 'header_widgets_test.dart' as helpers;

/// Opción "Todas las sucursales" del selector del cabecero.
///
/// El tri-state vive en [CompanyBranchState.allBranches]:
/// - `branchId != null` → una sucursal concreta (auto-seleccionada o elegida);
/// - `allBranches == true` → el usuario eligió EXPLÍCITAMENTE "Todas"
///   (sin X-Branch-Id → backend company-wide);
/// - ambos en `false`/`null` → nada seleccionado todavía → el header
///   auto-selecciona la primera sucursal de la lista.
///
/// El flag es lo que impide que la auto-selección deshaga "Todas".

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  group('CompanyBranchNotifier — tri-state Todas las sucursales', () {
    test('selectAllBranches activa el flag y limpia sucursal y bodega', () {
      final container = ProviderContainer();
      final n = container.read(companyBranchProvider.notifier);
      n.selectCompany('c1', 'ACME');
      n.selectBranch('b1', 'Sucursal 1');
      n.selectWarehouse('w1', 'Bodega 1');

      n.selectAllBranches();

      final s = container.read(companyBranchProvider);
      expect(s.allBranches, isTrue);
      expect(s.branchId, isNull);
      expect(s.branchName, isNull);
      expect(s.warehouseId, isNull,
          reason: 'las bodegas viven bajo una sucursal concreta');
      expect(s.companyId, 'c1', reason: 'la compañía se conserva');
      container.dispose();
    });

    test('selectBranch desactiva el flag (vuelta a sucursal concreta)', () {
      final container = ProviderContainer();
      final n = container.read(companyBranchProvider.notifier);
      n.selectCompany('c1', 'ACME');
      n.selectAllBranches();

      n.selectBranch('b2', 'Sucursal 2');

      final s = container.read(companyBranchProvider);
      expect(s.allBranches, isFalse);
      expect(s.branchId, 'b2');
      container.dispose();
    });

    test('cambiar de compañía reinicia a auto-selección; la misma la conserva',
        () {
      final container = ProviderContainer();
      final n = container.read(companyBranchProvider.notifier);
      n.selectCompany('c1', 'ACME');
      n.selectAllBranches();

      // Misma compañía → "Todas" se conserva.
      n.selectCompany('c1', 'ACME');
      expect(container.read(companyBranchProvider).allBranches, isTrue);

      // Otra compañía → vuelve a auto-seleccionar la primera sucursal.
      n.selectCompany('c2', 'OTRA');
      final s = container.read(companyBranchProvider);
      expect(s.allBranches, isFalse);
      expect(s.branchId, isNull);
      container.dispose();
    });

    test('clearBranch no altera la selección explícita Todas', () {
      final container = ProviderContainer();
      final n = container.read(companyBranchProvider.notifier);
      n.selectCompany('c1', 'ACME');
      n.selectAllBranches();

      // La limpieza de selección obsoleta solo aplica a sucursales concretas
      // (branchId != null); con "Todas" sale sin tocar el flag.
      n.clearBranch();

      final s = container.read(companyBranchProvider);
      expect(s.allBranches, isTrue);
      expect(s.branchId, isNull);
      container.dispose();
    });

    test('la hidratación restaura la selección Todas persistida', () async {
      SharedPreferences.setMockInitialValues({
        'selected_company_id': 'c1',
        'selected_company_name': 'ACME',
        'selected_branch_all': true,
      });

      final container = ProviderContainer();
      final n = container.read(companyBranchProvider.notifier);
      await n.whenHydrated;

      final s = container.read(companyBranchProvider);
      expect(s.allBranches, isTrue,
          reason: 'Todas elegida en una sesión previa debe persistir');
      expect(s.branchId, isNull);
      container.dispose();
    });

    test('persistencia: Todas se guarda y selectBranch la limpia', () async {
      final container = ProviderContainer();
      final n = container.read(companyBranchProvider.notifier);
      n.selectCompany('c1', 'ACME');

      n.selectAllBranches();
      await Future<void>.delayed(const Duration(milliseconds: 20));
      var prefs = await SharedPreferences.getInstance();
      expect(prefs.getBool('selected_branch_all'), isTrue);
      expect(prefs.getString('selected_branch_id'), isNull);

      n.selectBranch('b1', 'Sucursal 1');
      await Future<void>.delayed(const Duration(milliseconds: 20));
      prefs = await SharedPreferences.getInstance();
      expect(prefs.getBool('selected_branch_all'), isFalse);
      expect(prefs.getString('selected_branch_id'), 'b1');
      container.dispose();
    });
  });

  group('Selector del cabecero', () {
    testWidgets('auto-selecciona la primera sucursal si no hay selección',
        (tester) async {
      await helpers.setLargeScreenSize(tester);
      await tester.pumpWidget(helpers.buildTestApp(branches: [
        {'id': 'b1', 'name': 'Sucursal 1'},
        {'id': 'b2', 'name': 'Sucursal 2'},
      ]));
      // Future.value → data → post-frame auto-select → rebuild del chip.
      for (var i = 0; i < 4; i++) {
        await tester.pump();
      }

      final container =
          ProviderScope.containerOf(tester.element(find.byType(GlobalHeader)));
      final s = container.read(companyBranchProvider);
      expect(s.branchId, 'b1', reason: 'auto-selecciona la primera');
      expect(s.allBranches, isFalse);
      expect(find.text('Sucursal 1'), findsOneWidget);
    });

    testWidgets(
        'elegir Todas: chip con el label y la auto-selección no lo deshace',
        (tester) async {
      await helpers.setLargeScreenSize(tester);
      await tester.pumpWidget(helpers.buildTestApp(branches: [
        {'id': 'b1', 'name': 'Sucursal 1'},
        {'id': 'b2', 'name': 'Sucursal 2'},
      ]));
      for (var i = 0; i < 4; i++) {
        await tester.pump();
      }

      final container =
          ProviderScope.containerOf(tester.element(find.byType(GlobalHeader)));
      expect(container.read(companyBranchProvider).branchId, 'b1');

      // Abrir el selector y elegir "Todas las sucursales".
      await tester.tap(find.text('Sucursal 1'));
      await tester.pumpAndSettle();
      expect(find.text('Todas las sucursales'), findsOneWidget,
          reason: 'la opción aparece al tope del menú');
      await tester.tap(find.text('Todas las sucursales'));
      await tester.pumpAndSettle();

      var s = container.read(companyBranchProvider);
      expect(s.allBranches, isTrue);
      expect(s.branchId, isNull);
      expect(find.text('Todas las sucursales'), findsOneWidget,
          reason: 'el chip muestra el alcance activo');

      // Varios frames después NO se re-auto-selecciona la primera sucursal.
      await tester.pump(const Duration(milliseconds: 300));
      await tester.pump();
      await tester.pump();
      s = container.read(companyBranchProvider);
      expect(s.branchId, isNull,
          reason: 'el flag allBranches impide la re-auto-selección');
      expect(s.allBranches, isTrue);
      expect(find.text('Todas las sucursales'), findsOneWidget);
    });
  });
}
