import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/features/credit_notes/pages/credit_note_list_page.dart';
import 'package:zorvian/features/inventory_movements/pages/inventory_movement_list_page.dart';
import 'package:zorvian/features/purchase_orders/pages/purchase_order_list_page.dart';
import 'package:zorvian/features/purchase_requests/pages/purchase_request_list_page.dart';
import 'package:zorvian/features/service_payments/pages/service_payment_list_page.dart';
import 'package:zorvian/features/stock_transfers/pages/stock_transfer_list_page.dart';
import 'package:zorvian/features/supplier_credit_notes/pages/supplier_credit_note_list_page.dart';
import 'package:zorvian/features/supplier_payments/pages/supplier_payment_list_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

/// Listados operativos y comerciales a 360/390/430 px: órdenes de compra,
/// solicitudes de cotización, transferencias, notas de crédito (cliente y
/// proveedor), pagos a proveedores, pagos a prestadores y movimientos de
/// inventario.

Map<String, Object?> _routes() => {
      'purchase-orders': {
        'items': [
          {
            'id': 'po1',
            'orderNumber': 'OC-20261020-0001',
            'supplierName': 'Distribuidora La Unión S.A.',
            'createdAt': '2026-10-20T10:00:00',
            'status': 'approved',
            'total': 245300.0,
            'currencyCode': 'NIO',
          },
        ],
        'total': 1,
      },
      'purchase-requests': {
        'items': [
          {
            'id': 'pr1',
            'requestNumber': 'SC-20261020-0001',
            'title': 'Reposición de inventario para temporada alta',
            'priority': 'urgent',
            'status': 'submitted',
            'estimatedTotal': 98750.0,
            'createdAt': '2026-10-20T09:00:00',
            'branchName': 'Casa Matriz Managua',
          },
        ],
        'total': 1,
      },
      'stock-transfers': {
        'items': [
          {
            'id': 'st1',
            'transferNumber': 'TR-20261020-0001',
            'fromBranchName': 'Casa Matriz Managua',
            'toBranchName': 'Sucursal Granada',
            'fromWarehouseName': 'Bodega Principal de Productos Terminados',
            'toWarehouseName': 'Bodega Principal',
            'status': 'partially_received',
            'createdAt': '2026-10-20T08:00:00',
          },
        ],
        'total': 1,
      },
      'credit-notes': [
        {
          'id': 'cn1',
          'creditNoteNumber': 'NC-20261020-0001',
          'invoiceNumber': 'FAC-20261018-0042',
          'issueDate': '2026-10-20T00:00:00',
          'reason': 'Devolución de mercadería dañada en el transporte',
          'total': 12480.0,
          'status': 'issued',
        },
      ],
      'supplier-credit-notes': [
        {
          'id': 'scn1',
          'creditNoteNumber': 'NCP-20261020-0001',
          'supplierId': 'sup1',
          'supplierName': 'Distribuidora La Unión S.A.',
          'creditNoteDate': '2026-10-20T00:00:00',
          'reason': 'Producto recibido incompleto',
          'subtotal': 8000.0,
          'tax': 1200.0,
          'total': 9200.0,
          'status': 'applied',
        },
      ],
      'supplier-payments': [
        {
          'id': 'sp1',
          'purchaseId': 'pu1',
          'purchaseNumber': 'COM-20261018-0007',
          'amount': 45000.0,
          'paymentMethod': 'transfer',
          'referenceNumber': 'TRF-88231',
          'paymentDate': '2026-10-20T00:00:00',
        },
      ],
      'service-provider-payments': {
        'items': [
          {
            'id': 'svp1',
            'paymentNumber': 'PP-20261015-0001',
            'employeeId': 'e1',
            'employeeName': 'José Ramón Castillo',
            'employeeCode': 'EMP-0042',
            'branchId': 'br1',
            'branchName': 'Casa Matriz Managua',
            'periodYear': 2026,
            'periodMonth': 10,
            'periodHalf': 1,
            'periodLabel': 'Quincena 1 · Octubre 2026',
            'paymentDate': '2026-10-15T00:00:00',
            'status': 'paid',
            'goalMet': true,
            'totalAccumulated': 23026.76,
            'totalDeductions': 13923.36,
            'netAmount': 9103.40,
            'createdAt': '2026-10-15T09:00:00',
          },
        ],
        'total': 1,
      },
      'inventory-movements': {
        'items': [
          {
            'id': 'im1',
            'productName': 'Agua Purificada 600 ml (caja de 24)',
            'productCode': 'AGU-600-24',
            'type': 'transfer_out',
            'quantity': 12,
            'stockBefore': 120,
            'stockAfter': 108,
            'reference': 'TR-20261020-0001',
            'createdBy': 'Carlos Méndez',
            'createdAt': '2026-10-20T08:15:00',
          },
        ],
        'total': 1,
      },
      'branches': [
        {
          'id': 'br1',
          'name': 'Casa Matriz Managua',
          'code': 'SUC-001',
          'isActive': true,
        },
        {
          'id': 'br2',
          'name': 'Sucursal Granada',
          'code': 'SUC-002',
          'isActive': true,
        },
      ],
      'warehouses': [
        {
          'id': 'wh1',
          'branchId': 'br1',
          'branchName': 'Casa Matriz Managua',
          'name': 'Bodega Principal',
          'isActive': true,
          'isDefault': true,
        },
      ],
      'suppliers': [
        {
          'id': 'sup1',
          'name': 'Distribuidora La Unión S.A.',
          'identificationNumber': 'J0310000012345',
        },
      ],
      'employees': {
        'items': [
          {
            'id': 'e1',
            'fullName': 'José Ramón Castillo',
            'employeeCode': 'EMP-0042',
            'collaboratorType': 'service_provider',
          },
        ],
        'total': 1,
      },
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

  testWidgets('listado de órdenes de compra cabe en teléfono', (tester) async {
    await mount(tester, const PurchaseOrderListPage());

    expect(find.textContaining('OC-20261020-0001'), findsOneWidget);
  });

  testWidgets('listado de solicitudes de cotización cabe en teléfono',
      (tester) async {
    await mount(tester, const PurchaseRequestListPage());

    expect(find.textContaining('Reposición de inventario para temporada alta'),
        findsOneWidget);
  });

  testWidgets('listado de transferencias cabe en teléfono', (tester) async {
    await mount(tester, const StockTransferListPage());

    expect(find.textContaining('TR-20261020-0001'), findsOneWidget);
  });

  testWidgets('listado de notas de crédito cabe en teléfono', (tester) async {
    await mount(tester, const CreditNoteListPage());

    expect(find.textContaining('NC-20261020-0001'), findsOneWidget);
  });

  testWidgets('listado de notas de crédito de proveedor cabe en teléfono',
      (tester) async {
    await mount(tester, const SupplierCreditNoteListPage());

    expect(find.textContaining('NCP-20261020-0001'), findsOneWidget);
  });

  testWidgets('listado de pagos a proveedores cabe en teléfono', (tester) async {
    await mount(tester, const SupplierPaymentListPage());

    expect(find.textContaining('COM-20261018-0007'), findsOneWidget);
  });

  testWidgets('listado de pagos a prestadores cabe en teléfono', (tester) async {
    await mount(tester, const ServicePaymentListPage());

    expect(find.textContaining('José Ramón Castillo'), findsOneWidget);
  });

  testWidgets('listado de movimientos de inventario cabe en teléfono',
      (tester) async {
    await mount(tester, const InventoryMovementListPage());

    expect(find.textContaining('Agua Purificada 600 ml (caja de 24)'),
        findsOneWidget);
  });
}
