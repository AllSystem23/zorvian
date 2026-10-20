import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/core/offline/sync_engine.dart' show appDatabaseProvider;
import 'package:zorvian/features/accounting/pages/accounting_entries_page.dart';
import 'package:zorvian/features/cash_registers/pages/cash_register_list_page.dart';
import 'package:zorvian/features/clients/pages/client_list_page.dart';
import 'package:zorvian/features/credits/pages/credit_list_page.dart';
import 'package:zorvian/features/employees/pages/employee_list_page.dart';
import 'package:zorvian/features/products/pages/product_list_page.dart';
import 'package:zorvian/features/purchases/pages/purchase_list_page.dart';
import 'package:zorvian/features/quotes/pages/quote_list_page.dart';
import 'package:zorvian/features/sales/pages/sale_list_page.dart';
import 'package:zorvian/features/suppliers/pages/supplier_list_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/offline_db.dart';
import 'helpers/viewport.dart';

/// Barrido de los listados principales a 360/390/430 px.
///
/// El contrato que se prueba es el layout: `pumpAcrossPhones` falla con el
/// ancho en el mensaje si algo desborda. La API falsa responde una pagina
/// vacia, asi que se ejercita la barra de filtros + el estado vacio (el modo
/// tarjeta de `ZDataTable` ya tiene sus propios tests con filas reales).
void main() {
  final pages = <String, Widget Function()>{
    'ventas': () => const SaleListPage(),
    'productos': () => const ProductListPage(),
    'clientes': () => const ClientListPage(),
    'proveedores': () => const SupplierListPage(),
    'empleados': () => const EmployeeListPage(),
    'compras': () => const PurchaseListPage(),
    'cotizaciones': () => const QuoteListPage(),
    'créditos': () => const CreditListPage(),
    'asientos contables': () => const AccountingEntriesPage(),
    'cajas': () => const CashRegisterListPage(),
  };

  for (final entry in pages.entries) {
    testWidgets('listado de ${entry.key} cabe en teléfono', (tester) async {
      final api = FakeApi.routes(
        const {},
        fallback: const {
          'items': [],
          'totalCount': 0,
          'page': 1,
          'pageSize': 20,
          'totalPages': 0,
        },
      );
      // Para las pantallas offline-first (ventas, productos): el
      // `AppDatabase` real no abre en tests y la página se queda cargando.
      final db = memoryDb();
      addTearDown(db.close);

      await pumpAcrossPhones(
        tester,
        (_) => ProviderScope(
          overrides: [
            dioClientProvider.overrideWithValue(api.client),
            appDatabaseProvider.overrideWithValue(db),
          ],
          child: MaterialApp(home: entry.value()),
        ),
        height: 1200,
      );

      expect(find.byType(Scaffold), findsWidgets);
    });
  }
}
