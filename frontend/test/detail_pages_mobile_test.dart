import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/core/offline/app_database.dart';
import 'package:zorvian/core/offline/sync_engine.dart' show appDatabaseProvider;
import 'package:zorvian/features/budgets/pages/budget_vs_actual_detail_page.dart';
import 'package:zorvian/features/cash_registers/pages/cash_register_detail_page.dart';
import 'package:zorvian/features/credits/pages/credit_detail_page.dart';
import 'package:zorvian/features/crm/models/crm_models.dart';
import 'package:zorvian/features/crm/pages/lead_detail_page.dart';
import 'package:zorvian/features/crm/pages/opportunity_detail_page.dart';
import 'package:zorvian/features/crm/providers/leads_provider.dart';
import 'package:zorvian/features/crm/providers/opportunities_provider.dart';
import 'package:zorvian/features/documents/pages/document_detail_page.dart';
import 'package:zorvian/features/employees/pages/employee_detail_page.dart';
import 'package:zorvian/features/payroll/pages/payroll_run_detail_page.dart';
import 'package:zorvian/features/permissions/pages/permission_detail_page.dart';
import 'package:zorvian/features/providers/pages/provider_detail_page.dart';
import 'package:zorvian/features/providers/pages/service_contract_detail_page.dart';
import 'package:zorvian/features/purchase_orders/pages/purchase_order_detail_page.dart';
import 'package:zorvian/features/purchase_requests/pages/purchase_request_detail_page.dart';
import 'package:zorvian/features/purchases/pages/purchase_detail_page.dart';
import 'package:zorvian/features/quotes/pages/quote_detail_page.dart';
import 'package:zorvian/features/reconciliations/pages/reconciliation_detail_page.dart';
import 'package:zorvian/features/sales/pages/sale_detail_page.dart';
import 'package:zorvian/features/service_payments/pages/service_payment_detail_page.dart';
import 'package:zorvian/features/stock_transfers/pages/stock_transfer_detail_page.dart';
import 'package:zorvian/features/vacations/pages/vacation_detail_page.dart';
import 'package:zorvian/features/vacations/pages/vacation_receipt_detail_page.dart';
import 'package:zorvian/features/warranties/pages/warranty_detail_page.dart';
import 'package:zorvian/features/workshops/pages/workshop_detail_page.dart';

import 'helpers/detail_api.dart';
import 'helpers/fake_api.dart';
import 'helpers/offline_db.dart';
import 'helpers/viewport.dart';

/// `LeadNotifier` sembrado: el detalle de lead no pide la lista al API, la
/// lee de `leadsProvider` (la carga el listado). Sin sembrarla la página
/// mostraría "Prospecto no encontrado" y el test no probaría el layout.
class _SeededLeads extends LeadNotifier {
  @override
  LeadState build() => LeadState(
        leads: [
          Lead(
            id: 'l1',
            firstName: 'Ana',
            lastName: 'Ramírez',
            companyName: 'Comercial del Valle',
            jobTitle: 'Gerente de compras',
            email: 'ana.ramirez@example.com',
            phone: '8888-1111',
            city: 'Managua',
            countryCode: 'NI',
            source: 'Feria',
            interestLevel: 'high',
            status: 'new',
            notes: 'Interesada en flota',
            createdAt: DateTime(2026, 10, 1),
          ),
        ],
        totalCount: 1,
      );
}

/// Igual que [_SeededLeads] para el detalle de oportunidad.
class _SeededOpportunities extends OpportunityNotifier {
  @override
  OpportunityState build() => OpportunityState(
        stages: [
          PipelineStage(
            id: 'st1',
            name: 'Negociación',
            order: 1,
            color: '#7C4DFF',
          ),
        ],
        opportunities: [
          Opportunity(
            id: 'o1',
            title: 'Renovación de flota',
            description: 'Renovación de 3 vehículos',
            expectedValue: 25000,
            currencyCode: 'NIO',
            expectedCloseDate: DateTime(2026, 12, 15),
            probability: 60,
            stageId: 'st1',
            stageName: 'Negociación',
            status: 'open',
            priority: 'high',
            createdAt: DateTime(2026, 10, 1),
          ),
        ],
      );
}

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

  testWidgets('detalle de caja chica cabe en teléfono', (tester) async {
    await mount(tester, const CashRegisterDetailPage(registerId: 'cg1'));

    expect(find.text('CAJA-001'), findsWidgets);
    expect(find.textContaining('7500.00'), findsWidgets);

    // La pestaña de arqueo pinta filas etiqueta/valor y el desglose de
    // denominaciones (Spacer + montos), que no se dibujan al montar.
    await tester.tap(find.text('ARQUEO'));
    await tester.pumpAndSettle();

    expect(find.text('Resultado del Arqueo'), findsOneWidget);
    expect(find.textContaining('7480.00'), findsWidgets);
    expect(find.text('Billete C\$ 1000.00'), findsOneWidget);
  });

  testWidgets('detalle de corrida de nómina cabe en teléfono', (tester) async {
    await mount(tester, const PayrollRunDetailPage(runId: 'run1'));

    expect(find.text('Resumen de Nómina'), findsOneWidget);
    // Título + botones de exportación y la tabla de trabajadores.
    expect(find.text('Detalle por Trabajador'), findsOneWidget);
    expect(find.text('Planilla de Pago (PDF)'), findsOneWidget);
    expect(find.text('María López'), findsWidgets);
  });

  testWidgets('detalle de conciliación cabe en teléfono', (tester) async {
    await mount(tester, const ReconciliationDetailPage(reconciliationId: 'rec1'));

    // Sin conciliación seleccionada la página muestra el importador y el
    // aviso; los botones Subir CSV / Auto-Matching eran la fila que desbordaba.
    expect(find.text('Importar Estado de Cuenta'), findsOneWidget);
    expect(find.text('Subir CSV'), findsOneWidget);
    expect(find.text('Auto-Matching'), findsOneWidget);
    expect(find.text('Seleccione una conciliación para ver sus detalles'), findsOneWidget);
  });

  testWidgets('detalle de lead cabe en teléfono', (tester) async {
    // `ProviderScope` anidado: hereda el `dioClient` del scope externo y
    // siembra la lista de leads (la carga el listado, no el detalle).
    await mount(
      tester,
      ProviderScope(
        overrides: [leadsProvider.overrideWith(_SeededLeads.new)],
        child: const LeadDetailPage(leadId: 'l1'),
      ),
    );

    expect(find.text('Ana Ramírez'), findsWidgets);
    expect(find.text('Comercial del Valle'), findsWidgets);
  });

  testWidgets('detalle de oportunidad cabe en teléfono', (tester) async {
    await mount(
      tester,
      ProviderScope(
        overrides: [opportunitiesProvider.overrideWith(_SeededOpportunities.new)],
        child: const OpportunityDetailPage(opportunityId: 'o1'),
      ),
    );

    expect(find.text('Renovación de flota'), findsWidgets);
    expect(find.text('Negociación'), findsWidgets);
  });

  testWidgets('detalle de prestador de servicio cabe en teléfono', (tester) async {
    await mount(tester, const ProviderDetailPage(id: 'prov1'));

    expect(find.text('Servicios Técnicos del Norte'), findsWidgets);
    expect(find.textContaining('Mantenimiento'), findsWidgets);
  });

  testWidgets('detalle de contrato de servicio cabe en teléfono', (tester) async {
    await mount(tester, const ServiceContractDetailPage(id: 'sc1'));

    expect(find.textContaining('CS-2026-001'), findsWidgets);
    expect(find.textContaining('Mantenimiento preventivo'), findsWidgets);
    expect(find.text('Primer entregable'), findsWidgets);
  });

  testWidgets('detalle de taller cabe en teléfono', (tester) async {
    await mount(tester, const WorkshopDetailPage(workshopId: 'ws1'));

    expect(find.text('Taller Central'), findsWidgets);
    expect(find.text('Métricas de SLA'), findsOneWidget);
  });

  testWidgets('detalle de documento cabe en teléfono', (tester) async {
    await mount(tester, const DocumentDetailPage(documentId: 'doc1'));

    expect(find.text('Contrato de Trabajo - Juan Pérez'), findsWidgets);
    expect(find.text('Contrato de Trabajo'), findsWidgets);
  });

  testWidgets('detalle de permiso cabe en teléfono', (tester) async {
    await mount(tester, const PermissionDetailPage(permissionId: 'perm1'));

    expect(find.textContaining('Juan Pérez'), findsWidgets);
    expect(find.text('Personal'), findsWidgets);
    expect(find.text('Días totales'), findsOneWidget);
  });

  testWidgets('detalle de recibo de vacaciones cabe en teléfono', (tester) async {
    await mount(tester, const VacationReceiptDetailPage(receiptId: 'rv1'));

    expect(find.text('Juan Pérez'), findsWidgets);
    expect(find.text('RV-20261015-0001'), findsWidgets);
    // "Neto a Pagar:" + el monto (el Row que desbordaba 53 px).
    expect(find.text('Neto a Pagar:'), findsOneWidget);
  });

  testWidgets('detalle de presupuesto vs real cabe en teléfono', (tester) async {
    await mount(tester, const BudgetVsActualDetailPage(year: 2026, month: 10));

    expect(find.text('Presupuestado'), findsOneWidget);
    // El título de cada fila es "cuenta - nombre".
    expect(find.textContaining('Gastos de administración'), findsWidgets);
    expect(find.textContaining('10000.00'), findsWidgets);
  });
}
