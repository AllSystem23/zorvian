import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/core/offline/sync_engine.dart' show appDatabaseProvider;
import 'package:zorvian/features/permissions/pages/permission_form_page.dart';
import 'package:zorvian/features/purchase_orders/pages/purchase_order_form_page.dart';
import 'package:zorvian/features/purchase_requests/pages/purchase_request_form_page.dart';
import 'package:zorvian/features/purchases/pages/purchase_form_page.dart';
import 'package:zorvian/features/quotes/pages/quote_form_page.dart';
import 'package:zorvian/features/sales/pages/sale_form_page.dart';
import 'package:zorvian/features/service_payments/pages/service_payment_form_page.dart';
import 'package:zorvian/features/stock_transfers/pages/stock_transfer_form_page.dart';
import 'package:zorvian/features/vacations/pages/vacation_form_page.dart';
import 'package:zorvian/features/warranties/pages/warranty_form_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/offline_db.dart';
import 'helpers/viewport.dart';

/// Formularios transaccionales en modo creación a 360/390/430 px.
///
/// Varios leen `productProvider`, que es offline-first (Drift): sin una
/// `AppDatabase` en memoria la consulta nunca completa y la página se queda
/// en el spinner (y `pumpAndSettle` expira).
void main() {
  final forms = <String, Widget Function()>{
    'venta': () => const NewSalePage(),
    'cotización': () => const QuoteFormPage(),
    'compra': () => const PurchaseFormPage(),
    'orden de compra': () => const PurchaseOrderFormPage(),
    'solicitud de cotización': () => const PurchaseRequestFormPage(),
    'transferencia entre bodegas': () => const StockTransferFormPage(),
    'garantía': () => const WarrantyFormPage(),
    'permiso': () => const PermissionFormPage(),
    'vacación': () => const VacationFormPage(),
    'pago a prestador': () => const ServicePaymentFormPage(),
  };

  for (final entry in forms.entries) {
    testWidgets('formulario de ${entry.key} cabe en teléfono', (tester) async {
      final api = FakeApi.routes(
        const {},
        fallback: const {
          'items': [],
          'totalCount': 0,
          'page': 1,
          'pageSize': 50,
          'totalPages': 0,
        },
      );
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
        height: 2000,
      );

      expect(find.byType(Scaffold), findsWidgets);
    });
  }
}
