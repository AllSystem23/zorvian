import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/features/providers/pages/provider_contracts_page.dart';
import 'package:zorvian/features/providers/pages/provider_dashboard_page.dart';
import 'package:zorvian/features/providers/pages/provider_invoices_page.dart';
import 'package:zorvian/features/providers/pages/provider_list_page.dart';
import 'package:zorvian/features/settings/pages/company_settings_page.dart';
import 'package:zorvian/features/settings/pages/leave_type_form_page.dart';
import 'package:zorvian/features/settings/pages/leave_types_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

/// Barrido de prestadores de servicios y ajustes a 360/390/430 px.
///
/// Todo pasa por `ProviderRepository`/`dioClientProvider`, así que se sirve con
/// `FakeApi.routes(...)`. El dashboard de prestadores es el caso pesado: 4
/// `FutureProvider` propios + gráficos + listas con `trailing`.

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

Map<String, Object?> _provider({String status = 'active'}) => {
      'id': 'p1',
      'employeeId': 'e1',
      'businessName': 'Servicios Contables Brizuela y Asociados',
      'fiscalAddress': 'De la Rotonda Rubén Darío 2c al oeste, Managua',
      'taxRegime': 'cuota_fija',
      'professionalLicense': 'CPN-2019-0455',
      'specialization': 'Contabilidad y auditoría tributaria',
      'serviceCategory': 'Contabilidad',
      'insurancePolicy': 'INSS-RC-8891',
      'insuranceExpiration': '2027-03-31',
      'status': status,
    };

List<Map<String, Object?>> _providers() => [
      _provider(),
      _provider(status: 'inactive'),
    ];

List<Map<String, Object?>> _contracts() => [
      {
        'id': 'c1',
        'serviceProviderId': 'p1',
        'serviceProvider': _provider(),
        'contractNumber': 'CT-20261020-0001',
        'contractName': 'Asesoría contable mensual y declaraciones',
        'scope': 'Contabilidad general, planillas y declaraciones fiscales',
        'totalContractAmount': 495782.0,
        'currency': 'NIO',
        'paymentTerms': 'Quincenal',
        'startDate': '2026-01-15',
        'endDate': '2026-12-31',
        'status': 'active',
        'notes': 'Renovable anualmente con revisión de tarifas.',
      },
    ];

List<Map<String, Object?>> _invoices() => [
      {
        'id': 'i1',
        'paymentMilestoneId': 'm1',
        'invoiceNumber': 'F-0001245',
        'invoiceDate': '2026-10-15',
        'invoiceAmount': 247891.0,
        'withholdingAmount': 24789.1,
        'netAmount': 223101.9,
        'currency': 'NIO',
        'status': 'received',
      },
      {
        'id': 'i2',
        'paymentMilestoneId': 'm2',
        'invoiceNumber': 'F-0001198',
        'invoiceDate': '2026-09-30',
        'invoiceAmount': 120000.0,
        'withholdingAmount': 12000.0,
        'netAmount': 108000.0,
        'currency': 'NIO',
        'status': 'paid',
      },
    ];

Map<String, Object?> _providerDashboard() => {
      'totalProviders': 14,
      'activeProviders': 12,
      'totalContracts': 9,
      'activeContracts': 7,
      'totalContractValue': 4957820.0,
      'pendingMilestones': 6,
      'overdueMilestones': 2,
      'pendingInvoices': 3,
      'pendingInvoiceAmount': 743673.0,
      'milestonesByStatus': [
        {'status': 'pending', 'count': 4},
        {'status': 'in_progress', 'count': 2},
        {'status': 'completed', 'count': 3},
      ],
      'topProviders': [
        {
          'businessName': 'Servicios Contables Brizuela y Asociados',
          'contractCount': 3,
          'totalValue': 1487346.0,
        },
        {
          'businessName': 'Transportes del Norte S.A.',
          'contractCount': 2,
          'totalValue': 890000.0,
        },
      ],
      'recentContracts': [
        {
          'contractNumber': 'CT-20261020-0001',
          'contractName': 'Asesoría contable mensual y declaraciones',
          'providerName': 'Servicios Contables Brizuela y Asociados',
          'totalAmount': 495782.0,
          'status': 'active',
        },
      ],
    };

List<Map<String, Object?>> _providerNotifications() => [
      {
        'type': 'contract_expiring',
        'severity': 'high',
        'title': 'Contrato por vencer',
        'message': 'El contrato CT-20261020-0001 vence en 10 días y tiene '
            'hitos pendientes de aprobación por el área de compras.',
      },
      {
        'type': 'invoice_pending',
        'severity': 'medium',
        'title': 'Factura pendiente de pago',
        'message': 'La factura F-0001245 está registrada sin programar pago.',
      },
    ];

Map<String, Object?> _providerRankings() => {
      'averageOverallScore': 82.5,
      'rankings': [
        {
          'rank': 1,
          'businessName': 'Servicios Contables Brizuela y Asociados',
          'employeeName': 'José Ramón Castillo Vega',
          'overallScore': 94.5,
          'onTimeDeliveryScore': 96.0,
          'contractCompletionScore': 92.0,
          'invoiceAccuracyScore': 95.5,
        },
        {
          'rank': 2,
          'businessName': 'Transportes del Norte S.A.',
          'employeeName': 'María Fernanda López',
          'overallScore': 78.0,
          'onTimeDeliveryScore': 74.0,
          'contractCompletionScore': 80.0,
          'invoiceAccuracyScore': 80.0,
        },
      ],
    };

Map<String, Object?> _providerRankingHistory() => {
      'series': [
        {
          'businessName': 'Servicios Contables Brizuela y Asociados',
          'points': [
            {'month': '2026-05', 'overallScore': 80.0},
            {'month': '2026-06', 'overallScore': 84.0},
            {'month': '2026-07', 'overallScore': 88.0},
          ],
        },
        {
          'businessName': 'Transportes del Norte S.A.',
          'points': [
            {'month': '2026-05', 'overallScore': 70.0},
            {'month': '2026-06', 'overallScore': 74.0},
            {'month': '2026-07', 'overallScore': 78.0},
          ],
        },
      ],
    };

Map<String, Object?> _companySettings() => {
      'vacationDaysPerYear': 15,
      'lateToleranceMinutes': 15,
      'workingHoursPerDay': 8,
      'currency': 'NIO',
      'timezone': 'America/Managua',
      'lateFeeDailyRate': 0.001,
      'lateFeePercentage': 0.05,
      'lateFeeGracePeriod': 3,
      'taxEnabled': true,
      'taxRate': 0.15,
      'retentionBase': 'taxable_gross',
    };

List<Map<String, Object?>> _leaveTypes() => [
      {
        'code': 'VAC',
        'name': 'Vacaciones',
        'description': 'Descanso anual remunerado',
        'isPaid': true,
        'requiresAttachment': false,
        'requiresApproval': true,
        'maxDaysPerYear': 15,
      },
      {
        'code': 'SICK',
        'name': 'Enfermedad',
        'description': 'Reposo respaldado por el INSS',
        'isPaid': true,
        'requiresAttachment': true,
        'requiresApproval': false,
        'maxDaysPerYear': null,
      },
    ];

// ---------------------------------------------------------------------------
// Harness
// ---------------------------------------------------------------------------

/// Alto grande: con el alto real solo se construirían las primeras secciones de
/// los `ListView` y los overflows del final pasarían desapercibidos.
Future<void> _pumpPage(WidgetTester tester, Widget app) async {
  await pumpAcrossPhones(tester, (_) => app, height: 4000);
}

Widget _app(Map<String, Object?> routes, Widget page) {
  final api = FakeApi.routes(routes);
  return ProviderScope(
    overrides: [dioClientProvider.overrideWithValue(api.client)],
    child: MaterialApp(home: page),
  );
}

void main() {
  testWidgets('ProviderListPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _app({'providers': _providers()}, const ProviderListPage()),
    );

    expect(find.byType(ProviderListPage), findsOneWidget);
  });

  testWidgets('ProviderContractsPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _app(
        {'providers/contracts': _contracts()},
        const ProviderContractsPage(),
      ),
    );

    expect(find.byType(ProviderContractsPage), findsOneWidget);
  });

  testWidgets('ProviderInvoicesPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _app({'providers/invoices': _invoices()}, const ProviderInvoicesPage()),
    );

    expect(find.byType(ProviderInvoicesPage), findsOneWidget);
  });

  testWidgets('ProviderDashboardPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _app(
        {
          'providers/dashboard': _providerDashboard(),
          'providers/notifications': _providerNotifications(),
          'providers/rankings/history': _providerRankingHistory(),
          'providers/rankings': _providerRankings(),
        },
        const ProviderDashboardPage(),
      ),
    );

    expect(find.byType(ProviderDashboardPage), findsOneWidget);
  });

  testWidgets('CompanySettingsPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _app(
        {'companies/settings': _companySettings()},
        const CompanySettingsPage(),
      ),
    );

    expect(find.byType(CompanySettingsPage), findsOneWidget);
  });

  testWidgets('LeaveTypesPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _app({'leave-types': _leaveTypes()}, const LeaveTypesPage()),
    );

    expect(find.byType(LeaveTypesPage), findsOneWidget);
  });

  testWidgets('LeaveTypeFormPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _app(const {}, const LeaveTypeFormPage()),
    );

    expect(find.byType(LeaveTypeFormPage), findsOneWidget);
  });
}
