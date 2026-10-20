import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/core/offline/app_database.dart';
import 'package:zorvian/core/offline/sync_engine.dart' show appDatabaseProvider;
import 'package:zorvian/features/credits/pages/credit_detail_page.dart';
import 'package:zorvian/features/employees/pages/employee_detail_page.dart';
import 'package:zorvian/features/purchase_orders/pages/purchase_order_detail_page.dart';
import 'package:zorvian/features/purchase_requests/pages/purchase_request_detail_page.dart';
import 'package:zorvian/features/purchases/pages/purchase_detail_page.dart';
import 'package:zorvian/features/quotes/pages/quote_detail_page.dart';
import 'package:zorvian/features/sales/pages/sale_detail_page.dart';
import 'package:zorvian/features/service_payments/pages/service_payment_detail_page.dart';
import 'package:zorvian/features/stock_transfers/pages/stock_transfer_detail_page.dart';
import 'package:zorvian/features/vacations/pages/vacation_detail_page.dart';
import 'package:zorvian/features/warranties/pages/warranty_detail_page.dart';

import 'helpers/detail_api.dart';
import 'helpers/fake_api.dart';
import 'helpers/offline_db.dart';
import 'helpers/viewport.dart';

/// Pantallas de detalle de los documentos comerciales a 360/390/430 px.
///
/// No basta con que monten: las páginas de detalle pintan filas
/// etiqueta/valor con `Row(spaceBetween)` y barras de acciones con etiquetas
/// largas, así que se assertea que llegó el dato del documento (si el JSON
/// falla la página muestra "Error al cargar…" y el test no probaría el layout).
void main() {
  Future<void> mount(
    WidgetTester tester,
    Widget page, {
    double height = 1600,
    FakeApi? api,
  }) async {
    final fake = api ?? detailApi();
    final AppDatabase db = memoryDb();
    addTearDown(db.close);

    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [
          dioClientProvider.overrideWithValue(fake.client),
          appDatabaseProvider.overrideWithValue(db),
        ],
        child: MaterialApp(home: page),
      ),
      height: height,
    );
  }

  testWidgets('detalle de venta cabe en teléfono', (tester) async {
    await mount(tester, const SaleDetailPage(saleId: 's1'));

    expect(find.text('Cliente de Prueba'), findsWidgets);
    expect(find.text('F-0001'), findsWidgets);
    expect(find.text('Producto de prueba con nombre largo'), findsWidgets);
  });

  testWidgets('detalle de cotización cabe en teléfono', (tester) async {
    await mount(tester, const QuoteDetailPage(quoteId: 'q1'));

    expect(find.text('COT-0001'), findsWidgets);
    expect(find.text('Cliente de Prueba'), findsWidgets);
  });

  testWidgets('detalle de compra cabe en teléfono', (tester) async {
    await mount(tester, const PurchaseDetailPage(purchaseId: 'pu1'));

    // La barra de acciones trae "Generar Acta de Recepción (PDF)".
    expect(find.text('Generar Acta de Recepción (PDF)'), findsOneWidget);
    expect(find.text('FAC-778'), findsWidgets);
  });

  testWidgets('detalle de crédito cabe en teléfono', (tester) async {
    await mount(tester, const CreditDetailPage(creditId: 'cr1'));

    expect(find.text('Cliente de Prueba'), findsWidgets);
    expect(find.text('2 x \$575'), findsWidgets);
  });

  testWidgets('detalle de empleado cabe en teléfono', (tester) async {
    await mount(tester, const EmployeeDetailPage(employeeId: 'e1'));

    expect(find.text('Juan Pérez'), findsWidgets);
    expect(find.text('EMP-001'), findsWidgets);
  });

  testWidgets('detalle de transferencia entre bodegas cabe en teléfono', (tester) async {
    await mount(tester, const StockTransferDetailPage(stockTransferId: 'st1'));

    expect(find.text('TR-0001'), findsWidgets);
    expect(find.text('4/10 rec.'), findsOneWidget);
    expect(find.text('Bodega Principal'), findsWidgets);
  });

  testWidgets('detalle de solicitud de cotización cabe en teléfono', (tester) async {
    await mount(tester, const PurchaseRequestDetailPage(purchaseRequestId: 'pr1'));

    expect(find.text('SC-0001'), findsWidgets);
    expect(find.text('Prioridad: Alta'), findsOneWidget);
  });

  testWidgets('detalle de orden de compra cabe en teléfono', (tester) async {
    await mount(tester, const PurchaseOrderDetailPage(purchaseOrderId: 'po1'));

    expect(find.text('OC-0001'), findsWidgets);
    // El botón más largo de la barra de acciones.
    expect(find.text('Generar PDF (Motor Documental)'), findsOneWidget);
  });

  testWidgets('detalle de pago a prestador cabe en teléfono', (tester) async {
    await mount(tester, const ServicePaymentDetailPage(servicePaymentId: 'sp1'));

    expect(find.text('PP-0001'), findsWidgets);
    expect(find.text('Meta: SÍ'), findsWidgets);
    expect(find.text('Comprobante PDF'), findsOneWidget);
  });

  testWidgets('detalle de vacación cabe en teléfono', (tester) async {
    await mount(tester, const VacationDetailPage(vacationId: 'v1'));

    expect(find.text('Juan Pérez'), findsWidgets);
    expect(find.text('Días hábiles'), findsOneWidget);
  });

  testWidgets('detalle de garantía cabe en teléfono', (tester) async {
    await mount(tester, const WarrantyDetailPage(warrantyId: 'w1'));

    // Las pestañas son lo primero que se pinta de la garantía.
    expect(find.text('Reclamos'), findsWidgets);
    expect(find.text('Timeline'), findsWidgets);
  });
}
