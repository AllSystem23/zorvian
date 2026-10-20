import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/features/fleet/pages/fleet_delivery_form_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_document_form_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_driver_form_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_expense_form_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_fuel_form_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_maintenance_form_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_route_form_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_trip_form_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_vehicle_form_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_workshop_form_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

// ── Harness ──

Widget _expenseForm(FakeApi api) => ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(api.client)],
      child: const MaterialApp(home: FleetExpenseFormPage()),
    );

Widget _fuelForm(FakeApi api) => ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(api.client)],
      child: const MaterialApp(home: FleetFuelFormPage()),
    );

void main() {
  group('FleetExpenseFormPage', () {
    testWidgets('formulario de gasto cabe en teléfono', (tester) async {
      final api = FakeApi.routes({
        'fleet/expense-categories': [
          {'id': 'c1', 'name': 'Combustible y Lubricantes'},
          {'id': 'c2', 'name': 'Mantenimiento Preventivo'},
        ],
      });

      await pumpAcrossPhones(tester, (_) => _expenseForm(api), height: 1400);

      expect(find.text('Información del Gasto'), findsOneWidget);
      expect(find.text('Registrar gasto'), findsOneWidget);
      expect(find.text('Reembolsable'), findsOneWidget);
      expect(api.requestTo('fleet/expense-categories'), isNotNull);
    });

    testWidgets('formulario de gasto en edición cabe en teléfono',
        (tester) async {
      final api = FakeApi.routes({
        'fleet/expenses/e1': {
          'expenseDate': '2026-10-01T00:00:00',
          'description': 'Cambio de aceite y filtros del camión NPR',
          'amount': 3480.75,
          'currency': 'NIO',
          'paymentMethod': 'Card',
          'reimbursable': true,
          'expenseCategoryId': 'c1',
        },
        'fleet/expense-categories': [
          {'id': 'c1', 'name': 'Combustible y Lubricantes'},
        ],
        'fleet/expense-subcategories': [
          {'id': 's1', 'name': 'Aceite de motor'},
        ],
      });

      await pumpAcrossPhones(
        tester,
        (_) => ProviderScope(
          overrides: [dioClientProvider.overrideWithValue(api.client)],
          child: const MaterialApp(home: FleetExpenseFormPage(expenseId: 'e1')),
        ),
        height: 1400,
      );

      expect(find.text('Actualizar'), findsOneWidget);
    });
  });

  group('FleetFuelFormPage', () {
    testWidgets('formulario de combustible cabe en teléfono', (tester) async {
      final api = FakeApi.routes({});

      await pumpAcrossPhones(tester, (_) => _fuelForm(api), height: 1400);

      expect(find.text('Vehículo y Conductor'), findsOneWidget);
      expect(find.text('Carga'), findsOneWidget);
      expect(find.text('Registrar carga'), findsOneWidget);
      // Sin edición no se pide nada al backend.
      expect(api.requests, isEmpty);
    });
  });

  // Los 10 formularios de flota comparten `_buildSection`; este barrido
  // asegura que el encabezado (icono + título) y el cuerpo caben a 360 px.
  group('Formularios de flota', () {
    final forms = <String, Widget Function()>{
      'entrega': () => const FleetDeliveryFormPage(),
      'documento': () => const FleetDocumentFormPage(),
      'conductor': () => const FleetDriverFormPage(),
      'mantenimiento': () => const FleetMaintenanceFormPage(),
      'ruta': () => const FleetRouteFormPage(),
      'viaje': () => const FleetTripFormPage(),
      'vehículo': () => const FleetVehicleFormPage(),
      'taller': () => const FleetWorkshopFormPage(),
    };

    for (final entry in forms.entries) {
      testWidgets('formulario de ${entry.key} cabe en teléfono',
          (tester) async {
        final api = FakeApi.routes({});

        await pumpAcrossPhones(
          tester,
          (_) => ProviderScope(
            overrides: [dioClientProvider.overrideWithValue(api.client)],
            child: MaterialApp(home: entry.value()),
          ),
          height: 1600,
        );

        // Algunos piden catálogos en `initState` (vehículos, conductores,
        // tipos de documento); lo que importa es que el layout quepa.
        expect(find.byType(Form), findsOneWidget);
      });
    }
  });
}
