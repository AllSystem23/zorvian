import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/core/offline/app_database.dart';
import 'package:zorvian/core/offline/sync_engine.dart' show appDatabaseProvider;
import 'package:zorvian/features/credit_notes/pages/credit_note_form_page.dart';
import 'package:zorvian/features/credits/pages/credit_refinancing_form_page.dart';
import 'package:zorvian/features/payroll/pages/settlement_form_page.dart';
import 'package:zorvian/features/supplier_credit_notes/pages/supplier_credit_note_form_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/offline_db.dart';
import 'helpers/viewport.dart';

/// Formularios que solo existen con un documento previo (nota de crédito de
/// una venta/compra, refinanciamiento de un crédito) y la liquidación final,
/// a 360/390/430 px.
///
/// A diferencia de los formularios de alta, estos renderizan datos del
/// documento, así que la API falsa devuelve un JSON plausible: con una lista
/// vacía solo se probaría el estado de error.
void main() {
  Widget app(FakeApi api, AppDatabase db, Widget home) => ProviderScope(
        overrides: [
          dioClientProvider.overrideWithValue(api.client),
          appDatabaseProvider.overrideWithValue(db),
        ],
        child: MaterialApp(home: home),
      );

  testWidgets('nota de crédito de venta cabe en teléfono', (tester) async {
    final api = FakeApi.routes(
      const {
        'sales/s1': {
          'id': 's1',
          'invoiceNumber': 'F-0001',
          'clientName': 'Cliente de Prueba',
          'total': 1000.0,
          'details': [
            {
              'productId': 'p1',
              'productName': 'Producto de prueba con nombre largo',
              'unitPrice': 250.0,
              'quantity': 4,
            },
          ],
        },
      },
      fallback: const {'items': [], 'totalCount': 0, 'totalPages': 0},
    );
    final db = memoryDb();
    addTearDown(db.close);

    await pumpAcrossPhones(
      tester,
      (_) => app(api, db, const CreditNoteFormPage(saleId: 's1')),
      height: 1200,
    );

    expect(find.text('Producto de prueba con nombre largo'), findsOneWidget);
  });

  testWidgets('nota de crédito de proveedor cabe en teléfono', (tester) async {
    final api = FakeApi.routes(
      const {
        'purchases/pu1': {'supplierId': 'sup1', 'supplierName': 'Proveedor de Prueba'},
      },
      fallback: const {'items': [], 'totalCount': 0, 'totalPages': 0},
    );
    final db = memoryDb();
    addTearDown(db.close);

    await pumpAcrossPhones(
      tester,
      (_) => app(api, db, const SupplierCreditNoteFormPage(purchaseId: 'pu1')),
      height: 1200,
    );

    expect(api.requestTo('purchases/pu1'), isNotNull);
  });

  testWidgets('refinanciamiento de crédito cabe en teléfono', (tester) async {
    final api = FakeApi.routes(
      const {},
      fallback: const {'items': [], 'totalCount': 0, 'totalPages': 0},
    );
    final db = memoryDb();
    addTearDown(db.close);

    await pumpAcrossPhones(
      tester,
      (_) => app(api, db, const CreditRefinancingFormPage(creditId: 'cr1')),
      height: 1200,
    );

    // No consulta el crédito: solo lo envía al guardar.
    expect(api.requests, isEmpty);
  });

  testWidgets('liquidación final cabe en teléfono', (tester) async {
    final api = FakeApi.routes(
      const {
        'employees/e1': {
          'id': 'e1',
          'firstName': 'Juan',
          'lastName': 'Pérez',
          'code': 'EMP-001',
          'position': 'Vendedor',
          'hireDate': '2020-01-15T00:00:00',
          'baseSalary': 15000.0,
          'status': 'active',
        },
      },
      fallback: const {'items': [], 'totalCount': 0, 'totalPages': 0},
    );
    final db = memoryDb();
    addTearDown(db.close);

    await pumpAcrossPhones(
      tester,
      (_) => app(api, db, const SettlementFormPage(employeeId: 'e1', companyId: 'co1')),
      height: 1600,
    );

    expect(find.text('Configuración de Liquidación'), findsOneWidget);
    expect(api.requestTo('employees/e1'), isNotNull);
  });
}
