import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/features/accounting/pages/fiscal_years_page.dart';
import 'package:zorvian/features/approval/pages/approval_flow_list_page.dart';
import 'package:zorvian/features/approval/pages/approval_pending_page.dart';
import 'package:zorvian/features/approval/providers/approval_provider.dart';
import 'package:zorvian/features/attendance/pages/kiosk_page.dart';
import 'package:zorvian/features/attendance/pages/qr_checkin_page.dart';
import 'package:zorvian/features/chat/pages/chat_page.dart';
import 'package:zorvian/features/custom_reports/models/report_designer_models.dart';
import 'package:zorvian/features/custom_reports/pages/custom_report_builder_page.dart';
import 'package:zorvian/features/custom_reports/pages/custom_report_list_page.dart';
import 'package:zorvian/features/custom_reports/pages/custom_report_result_page.dart';
import 'package:zorvian/features/custom_reports/providers/custom_report_provider.dart';
import 'package:zorvian/features/onboarding/onboarding_page.dart';
import 'package:zorvian/features/predictions/pages/sales_predictions_page.dart';
import 'package:zorvian/features/profile/profile_page.dart';
import 'package:zorvian/features/warranties/pages/warranty_tracking_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

/// Pantallas "sueltas" del barrido móvil: aprobaciones, reportes
/// personalizados, perfil, onboarding, chat, predicciones, seguimiento de
/// garantías, años fiscales y kiosco de asistencia.
///
/// Se montan a 360/390/430 px (ver `phoneWidths`). Cualquier `RenderFlex
/// overflow` aparece como excepción y hace fallar el caso automáticamente.

Map<String, Object?> _routes() => {
      'approval-requests/pending': [
        {
          'id': 'ar1',
          'module': 'Sale',
          'eventType': 'SaleDiscountApproval',
          'status': 'pending',
          'currentStep': 2,
          'totalSteps': 3,
          'requestedBy': 'María Fernanda López',
          'requestedAt': '2026-10-20T10:00:00',
        },
      ],
      'approval-flows': [
        {
          'id': 'af1',
          'module': 'Sale',
          'eventType': 'SaleDiscountApproval',
          'description': 'Descuentos mayores al 10% en ventas de contado',
          'isActive': true,
          'steps': [
            {'id': 's1', 'stepOrder': 1, 'approverRole': 'Manager'},
          ],
        },
        {
          'id': 'af2',
          'module': 'Purchase',
          'eventType': 'PurchaseApproval',
          'description': '',
          'isActive': false,
          'steps': [],
        },
      ],
      'custom-reports': [
        {
          'id': 'cr1',
          'name': 'Ventas por sucursal del mes',
          'description': 'Detalle de ventas agrupadas por sucursal',
          'module': 'sales',
          'fields': [
            {
              'name': 'Total',
              'source': 'total',
              'dataType': 'number',
              'visible': true,
              'order': 0,
              'aggregate': 'sum',
            },
          ],
          'filters': [],
          'sortOrder': 'asc',
          'isPublic': false,
        },
      ],
      'fiscal-years': [
        {
          'id': 'fy1',
          'year': 2026,
          'name': 'Año Fiscal 2026',
          'status': 'open',
        },
        {
          'id': 'fy2',
          'year': 2025,
          'name': 'Año Fiscal 2025',
          'status': 'audited',
          'closedAt': '2025-12-31',
        },
      ],
      'sales/predictions/next-month': {
        'predictedTotal': 125000.0,
        'confidence': 87.5,
        'byCategory': [
          {'category': 'Bebidas', 'amount': 45000.0},
          {'category': 'Abarrotes', 'amount': 80000.0},
        ],
      },
      'certificate-requests/employees/me/certificate-requests': [
        {
          'id': 'crq1',
          'certificateType': 'salarial',
          'reason': 'Trámite bancario',
          'status': 'pending',
          'requestedAt': '2026-10-15T09:00:00',
        },
      ],
      'employees/me': {
        'fullName': 'María Fernanda López',
        'employeeCode': 'EMP-0001',
        'email': 'maria.lopez@example.com',
        'phone': '+505 8888 0000',
        'dateOfBirth': '1992-03-14',
        'gender': 'Femenino',
        'identificationNumber': '001-140392-0001A',
        'position': 'Gerente de Operaciones',
        'department': 'Administración',
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

/// Reportes personalizados ya cargados (el listado real espera a que el
/// contenedor llame `load()`, igual que en la app).
class _SeededCustomReports extends CustomReportNotifier {
  @override
  CustomReportState build() => const CustomReportState(
        items: [
          CustomReportItem(
            id: 'cr1',
            name: 'Ventas por sucursal del mes',
            description: 'Detalle de ventas agrupadas por sucursal',
            module: 'sales',
            fields: [
              ReportField(
                name: 'Total',
                source: 'total',
                dataType: 'number',
                visible: true,
                order: 0,
                aggregate: 'sum',
              ),
            ],
          ),
        ],
      );
}

/// Carga los flujos antes de mostrar la pantalla (en la app lo hace el shell).
class _LoadFlowsBootstrap extends ConsumerStatefulWidget {
  const _LoadFlowsBootstrap({required this.child});

  final Widget child;

  @override
  ConsumerState<_LoadFlowsBootstrap> createState() => _LoadFlowsBootstrapState();
}

class _LoadFlowsBootstrapState extends ConsumerState<_LoadFlowsBootstrap> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(approvalProvider.notifier).loadFlows());
  }

  @override
  Widget build(BuildContext context) => widget.child;
}

void main() {
  // `Override` no lo exporta `flutter_riverpod.dart` (solo `riverpod`), así
  // que la lista se tipa como `dynamic` y se esparce sin fricción.
  Future<void> mount(
    WidgetTester tester,
    Widget page, {
    List<dynamic> overrides = const [],
    double height = 1200,
  }) async {
    final api = FakeApi.routes(_routes(), fallback: _page());

    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [
          dioClientProvider.overrideWithValue(api.client),
          ...overrides,
        ],
        child: MaterialApp(home: page),
      ),
      height: height,
    );
  }

  testWidgets('bandeja de aprobaciones cabe en teléfono', (tester) async {
    await mount(tester, const ApprovalPendingPage());

    expect(find.textContaining('Solicitado por'), findsOneWidget);
  });

  testWidgets('flujos de aprobación caben en teléfono', (tester) async {
    await mount(
      tester,
      const _LoadFlowsBootstrap(child: ApprovalFlowListPage()),
    );

    expect(find.textContaining('Descuentos mayores'), findsOneWidget);
  });

  testWidgets('lista de reportes personalizados cabe en teléfono',
      (tester) async {
    await mount(
      tester,
      const CustomReportListPage(),
      overrides: [
        customReportProvider.overrideWith(_SeededCustomReports.new),
      ],
    );

    expect(find.text('Ventas por sucursal del mes'), findsOneWidget);
  });

  testWidgets('constructor de reportes personalizados cabe en teléfono',
      (tester) async {
    await mount(tester, const CustomReportBuilderPage(), height: 1800);

    // Agregar un campo ejercita la fila frágil (arrastre + visibilidad +
    // agregado + borrar) del `ReorderableListView`.
    final chip = find.byType(ActionChip).first;
    await tester.tap(chip);
    await tester.pumpAndSettle();

    expect(find.text('Guardar Reporte'), findsOneWidget);
  });

  testWidgets('resultado de reporte personalizado cabe en teléfono',
      (tester) async {
    await mount(
      tester,
      const CustomReportResultPage(
        reportName: 'Ventas por sucursal del mes',
        module: 'sales',
        fields: [],
        columns: ['Sucursal', 'Ventas del mes', 'Cobros del mes'],
        rows: [
          {
            'Sucursal': 'Casa Matriz Managua',
            'Ventas del mes': 125000.0,
            'Cobros del mes': 98000.0,
          },
        ],
      ),
    );

    expect(find.text('Casa Matriz Managua'), findsOneWidget);
  });

  testWidgets('mi perfil cabe en teléfono', (tester) async {
    await mount(tester, const ProfilePage());

    expect(find.text('Mi Perfil'), findsOneWidget);
    expect(find.text('EMP-0001'), findsOneWidget);
  });

  testWidgets('onboarding cabe en teléfono', (tester) async {
    await mount(tester, const OnboardingPage(), height: 1600);

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('chat cabe en teléfono', (tester) async {
    await mount(tester, const ChatPage());

    expect(find.textContaining('Pregunta sobre tus datos'), findsOneWidget);
  });

  testWidgets('predicción de ventas cabe en teléfono', (tester) async {
    await mount(tester, const SalesPredictionsPage());

    expect(find.textContaining('Ventas Proyectadas'), findsOneWidget);
    expect(find.text('Bebidas'), findsOneWidget);
  });

  testWidgets('seguimiento de garantía cabe en teléfono', (tester) async {
    await mount(tester, const WarrantyTrackingPage(), height: 1400);

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('años fiscales caben en teléfono', (tester) async {
    await mount(tester, const FiscalYearsPage());

    expect(find.text('Año Fiscal 2026'), findsOneWidget);
    expect(find.text('Año Fiscal 2025'), findsOneWidget);
  });

  testWidgets('escáner QR de asistencia cabe en teléfono', (tester) async {
    // El visor de cámara (`mobile_scanner`) no existe en el entorno de test,
    // pero la pantalla degrada a un hueco: el overlay de 250 px y las guías
    // se pueden medir igual a 360/390/430 px.
    await mount(tester, const QRCheckInPage());

    expect(find.textContaining('Apunta al código QR'), findsOneWidget);
  });

  testWidgets('kiosco de asistencia cabe en teléfono', (tester) async {
    // El kiosco tiene un `Timer.periodic` de 1 s para el reloj, así que no se
    // puede usar `pumpAndSettle` (nunca se asienta). Se bombea a mano y se
    // desmonta al final para cancelar el timer.
    final api = FakeApi.routes(_routes(), fallback: _page());

    for (final width in phoneWidths) {
      await pumpAt(
        tester,
        width,
        ProviderScope(
          overrides: [dioClientProvider.overrideWithValue(api.client)],
          child: const MaterialApp(home: KioskPage()),
        ),
        height: 1400,
        settle: false,
      );
      await tester.pump(const Duration(milliseconds: 100));
      expect(
        tester.takeException(),
        isNull,
        reason: 'Desborde de layout a ${width.toInt()} px',
      );
    }

    await tester.pumpWidget(const SizedBox.shrink());
  });
}
