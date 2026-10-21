import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/features/branches/pages/branch_list_page.dart';
import 'package:zorvian/features/brands/pages/brand_list_page.dart';
import 'package:zorvian/features/categories/pages/category_list_page.dart';
import 'package:zorvian/features/cost_centers/pages/cost_center_list_page.dart';
import 'package:zorvian/features/exchange_rates/pages/exchange_rate_list_page.dart';
import 'package:zorvian/features/warehouses/pages/warehouse_list_page.dart';
import 'package:zorvian/features/workshops/pages/workshop_list_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

/// Listados de catálogo a 360/390/430 px: marcas, categorías, centros de
/// costo, tipos de cambio, sucursales, bodegas y talleres.
///
/// Todos cargan en `initState` contra `FakeApi`, así que se miden con datos
/// reales dentro de las tarjetas (que es donde aparecen los overflows).

Map<String, Object?> _routes() => {
      'brands': [
        {
          'id': 'br1',
          'name': 'Marca Premium del Pacífico',
          'description': 'Línea de bebidas importadas y distribución exclusiva',
          'isActive': true,
        },
      ],
      'categories': [
        {
          'id': 'cat1',
          'name': 'Abarrotes y productos de consumo masivo',
          'description': 'Productos de canasta básica familiar',
          'isActive': true,
        },
      ],
      'cost-centers': [
        {
          'id': 'cc1',
          'code': 'CC-ADM-01',
          'name': 'Administración Central Managua',
          'description': 'Gastos operativos de la oficina central',
          'isActive': true,
        },
      ],
      'exchange-rates': [
        {
          'id': 'er1',
          'fromCurrency': 'USD',
          'toCurrency': 'NIO',
          'rate': 36.6245,
          'effectiveDate': '2026-10-20T00:00:00',
        },
      ],
      'branches': [
        {
          'id': 'br-suc1',
          'name': 'Casa Matriz Managua',
          'code': 'SUC-001',
          'phone': '+505 2222 0000',
          'address': 'Km 4 Carretera a Masaya, Managua',
          'isActive': true,
        },
        {
          'id': 'br-suc2',
          'name': 'Sucursal Granada',
          'code': 'SUC-002',
          'isActive': false,
        },
      ],
      'warehouses': [
        {
          'id': 'wh1',
          'branchId': 'br-suc1',
          'branchName': 'Casa Matriz Managua',
          'name': 'Bodega Principal de Productos Terminados',
          'code': 'BOD-001',
          'address': 'Km 4 Carretera a Masaya, portón 3',
          'isActive': true,
          'isDefault': true,
        },
        {
          'id': 'wh2',
          'branchId': 'br-suc1',
          'branchName': 'Casa Matriz Managua',
          'name': 'Bodega de Devoluciones',
          'code': 'BOD-002',
          'isActive': false,
          'isDefault': false,
        },
      ],
      'service-workshops': [
        {
          'id': 't1',
          'code': 'TAL-001',
          'name': 'Taller Mecánico Los Pinos',
          'contactName': 'Carlos Méndez',
          'phone': '+505 8888 1234',
          'email': 'contacto@lospinos.example.com',
          'city': 'Managua',
          'avgResponseHours': 24,
          'avgRepairHours': 48,
          'rating': 4.6,
          'isActive': true,
          'technicianCount': 7,
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
      height: 1000,
    );
  }

  testWidgets('listado de marcas cabe en teléfono', (tester) async {
    await mount(tester, const BrandListPage());

    expect(find.text('Marca Premium del Pacífico'), findsOneWidget);
  });

  testWidgets('listado de categorías cabe en teléfono', (tester) async {
    await mount(tester, const CategoryListPage());

    expect(find.text('Abarrotes y productos de consumo masivo'), findsOneWidget);
  });

  testWidgets('listado de centros de costo cabe en teléfono', (tester) async {
    await mount(tester, const CostCenterListPage());

    expect(find.text('Administración Central Managua'), findsOneWidget);
  });

  testWidgets('listado de tipos de cambio cabe en teléfono', (tester) async {
    await mount(tester, const ExchangeRateListPage());

    expect(find.text('USD → NIO'), findsOneWidget);
  });

  testWidgets('listado de sucursales cabe en teléfono', (tester) async {
    await mount(tester, const BranchListPage());

    expect(find.text('Casa Matriz Managua'), findsOneWidget);
    expect(find.text('Inactiva'), findsOneWidget);
  });

  testWidgets('listado de bodegas cabe en teléfono', (tester) async {
    await mount(tester, const WarehouseListPage());

    expect(find.text('Bodega Principal de Productos Terminados'), findsOneWidget);
    // La segunda bodega lleva dos chips (Principal no, Inactiva sí) y es el
    // caso que más aprieta la fila del título.
    expect(find.text('Inactiva'), findsOneWidget);
  });

  testWidgets('listado de talleres cabe en teléfono', (tester) async {
    await mount(tester, const WorkshopListPage());

    expect(find.text('Taller Mecánico Los Pinos'), findsOneWidget);
    expect(find.text('4.6'), findsOneWidget);
  });
}
