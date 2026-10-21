import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/features/fleet/pages/fleet_delivery_list_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_document_list_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_driver_list_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_expense_list_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_fuel_list_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_maintenance_list_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_route_list_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_trip_list_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_vehicle_list_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_workshop_list_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

/// Los 10 listados de flota a 360/390/430 px.

Map<String, Object?> _routes() => {
      'fleet/vehicles': {
        'items': [
          {
            'id': 'v1',
            'code': 'VEH-001',
            'plate': 'M 123 456',
            'brandName': 'Toyota',
            'model': 'Hilux 2.8 4x4',
            'year': 2023,
            'color': 'Blanco',
            'vin': '8AJBA3CD9P1234567',
            'currentKm': 74210.5,
            'status': 'Active',
            'driverName': 'José Ramón Castillo',
            'branchName': 'Casa Matriz Managua',
          },
        ],
        'total': 1,
      },
      'fleet/drivers': {
        'items': [
          {
            'id': 'd1',
            'firstName': 'José Ramón',
            'lastName': 'Castillo Vega',
            'fullName': 'José Ramón Castillo Vega',
            'idDocument': '001-140392-0001A',
            'phone': '+505 8888 0000',
            'email': 'jose.castillo@example.com',
            'licenseNumber': 'LIC-88231',
            'licenseExpiryDate': '2027-04-30T00:00:00',
            'status': 'Active',
            'branchName': 'Casa Matriz Managua',
            'licenseCategoryName': 'Profesional',
          },
        ],
        'total': 1,
      },
      'fleet/routes': {
        'items': [
          {
            'id': 'r1',
            'code': 'RUT-001',
            'name': 'Ruta Managua – Granada – Masaya',
            'type': 'Delivery',
            'scheduledDate': '2026-10-21T06:00:00',
            'status': 'Planned',
            'vehiclePlate': 'M 123 456',
            'driverName': 'José Ramón Castillo',
            'originAddress': 'Km 4 Carretera a Masaya, Managua',
            'destinationAddress': 'Calle Real Xalteva, Granada',
            'createdAt': '2026-10-20T09:00:00',
          },
        ],
        'total': 1,
      },
      'fleet/trips': {
        'items': [
          {
            'id': 't1',
            'code': 'VIA-001',
            'vehiclePlate': 'M 123 456',
            'vehicleBrandModel': 'Toyota Hilux 2.8 4x4',
            'driverName': 'José Ramón Castillo',
            'startDateTime': '2026-10-21T06:00:00',
            'endDateTime': '2026-10-21T14:30:00',
            'origin': 'Managua',
            'destination': 'Granada',
            'status': 'Completed',
            'startKm': 74210.5,
            'endKm': 74350.0,
            'totalKm': 139.5,
            'createdAt': '2026-10-21T05:00:00',
          },
        ],
        'total': 1,
      },
      'fleet/deliveries': {
        'items': [
          {
            'id': 'dl1',
            'code': 'ENT-001',
            'clientName': 'Comercial Los Pinos S.A.',
            'deliveryAddress': 'Km 12 Carretera Norte, Managua',
            'scheduledDate': '2026-10-21T08:00:00',
            'vehiclePlate': 'M 123 456',
            'driverName': 'José Ramón Castillo',
            'status': 'Pending',
            'createdAt': '2026-10-20T09:00:00',
          },
        ],
        'total': 1,
      },
      'fleet/fuel-refills': {
        'items': [
          {
            'id': 'fr1',
            'refillDateTime': '2026-10-21T07:15:00',
            'vehiclePlate': 'M 123 456',
            'vehicleBrandModel': 'Toyota Hilux 2.8 4x4',
            'driverName': 'José Ramón Castillo',
            'fuelTypeName': 'Diésel',
            'liters': 45.5,
            'pricePerLiter': 34.2,
            'totalCost': 1556.1,
            'currentKm': 74210.5,
            'refillType': 'Full',
            'paymentMethod': 'Cash',
            'anomalyFlag': false,
            'supplierName': 'Gasolinera Uno',
            'createdAt': '2026-10-21T07:15:00',
          },
        ],
        'total': 1,
      },
      'fleet/work-orders': {
        'items': [
          {
            'id': 'wo1',
            'number': 'OT-20261021-0001',
            'vehiclePlate': 'M 123 456',
            'vehicleBrandModel': 'Toyota Hilux 2.8 4x4',
            'driverName': 'José Ramón Castillo',
            'reportDateTime': '2026-10-21T08:00:00',
            'failureTypeName': 'Frenos',
            'problemDescription': 'Ruido en la pastilla delantera derecha',
            'priority': 'High',
            'status': 'InProgress',
            'workshopName': 'Taller Mecánico Los Pinos',
            'mechanicResponsible': 'Carlos Méndez',
            'costEst': 3200,
            'costTotal': 0,
            'createdAt': '2026-10-21T08:00:00',
          },
        ],
        'total': 1,
      },
      'fleet/expenses': {
        'items': [
          {
            'id': 'fe1',
            'expenseDate': '2026-10-21T00:00:00',
            'categoryName': 'Mantenimiento',
            'subcategoryName': 'Repuestos',
            'vehiclePlate': 'M 123 456',
            'vehicleBrandModel': 'Toyota Hilux 2.8 4x4',
            'driverName': 'José Ramón Castillo',
            'description': 'Juego de pastillas de freno delanteras',
            'amount': 1850.0,
            'currency': 'NIO',
            'amountBaseCurrency': 1850.0,
            'paymentMethod': 'Cash',
            'reimbursable': true,
            'reimbursed': false,
            'approved': false,
            'createdAt': '2026-10-21T09:00:00',
          },
        ],
        'total': 1,
      },
      'fleet/documents': {
        'items': [
          {
            'id': 'fd1',
            'entityType': 'Vehicle',
            'entityId': 'v1',
            'documentTypeId': 'dt1',
            'documentTypeName': 'SOAT / Seguro obligatorio',
            'documentTypeHasExpiry': true,
            'documentNumber': 'SOAT-2026-88231',
            'issueDate': '2026-01-01T00:00:00',
            'expiryDate': '2027-01-01T00:00:00',
            'status': 'Valid',
            'alertSent': false,
            'createdAt': '2026-01-01T00:00:00',
          },
        ],
        'total': 1,
      },
      'fleet/workshops': [
        {
          'id': 'fw1',
          'name': 'Taller Mecánico Los Pinos',
          'contactPerson': 'Carlos Méndez',
          'phone': '+505 8888 1234',
          'email': 'contacto@lospinos.example.com',
          'address': 'Km 7 Carretera Sur, Managua',
          'isInternal': false,
          'isActive': true,
        },
      ],
    };

Map<String, Object?> _page() => {
      'items': <Object?>[],
      'data': <Object?>[],
      'totalCount': 0,
      'page': 1,
      'pageSize': 50,
      'totalPages': 0,
    };

void main() {
  Future<void> mount(WidgetTester tester, Widget page) async {
    final api = FakeApi.routes(_routes(), fallback: _page());

    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [dioClientProvider.overrideWithValue(api.client)],
        child: MaterialApp(home: page),
      ),
      height: 1100,
    );
  }

  testWidgets('listado de vehículos cabe en teléfono', (tester) async {
    await mount(tester, const FleetVehicleListPage());
    expect(find.textContaining('Toyota'), findsWidgets);
  });

  testWidgets('listado de conductores cabe en teléfono', (tester) async {
    await mount(tester, const FleetDriverListPage());
    expect(find.textContaining('José Ramón Castillo'), findsWidgets);
  });

  testWidgets('listado de rutas cabe en teléfono', (tester) async {
    await mount(tester, const FleetRouteListPage());
    expect(find.textContaining('RUT-001'), findsWidgets);
  });

  testWidgets('listado de viajes cabe en teléfono', (tester) async {
    await mount(tester, const FleetTripListPage());
    expect(find.textContaining('VIA-001'), findsWidgets);
  });

  testWidgets('listado de entregas cabe en teléfono', (tester) async {
    await mount(tester, const FleetDeliveryListPage());
    expect(find.textContaining('ENT-001'), findsWidgets);
  });

  testWidgets('listado de combustible cabe en teléfono', (tester) async {
    await mount(tester, const FleetFuelListPage());
    expect(find.textContaining('Diésel'), findsWidgets);
  });

  testWidgets('listado de órdenes de trabajo cabe en teléfono', (tester) async {
    await mount(tester, const FleetMaintenanceListPage());
    expect(find.textContaining('OT-20261021-0001'), findsWidgets);
  });

  testWidgets('listado de gastos de flota cabe en teléfono', (tester) async {
    await mount(tester, const FleetExpenseListPage());
    expect(find.textContaining('pastillas de freno'), findsWidgets);
  });

  testWidgets('listado de documentos de flota cabe en teléfono', (tester) async {
    await mount(tester, const FleetDocumentListPage());
    expect(find.textContaining('SOAT-2026-88231'), findsWidgets);
  });

  testWidgets('listado de talleres de flota cabe en teléfono', (tester) async {
    await mount(tester, const FleetWorkshopListPage());
    expect(find.textContaining('Taller Mecánico Los Pinos'), findsWidgets);
  });
}
