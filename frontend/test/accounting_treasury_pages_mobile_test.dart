import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/features/accounting/pages/account_links_page.dart';
import 'package:zorvian/features/accounting/pages/accounting_periods_page.dart';
import 'package:zorvian/features/accounting/pages/chart_of_accounts_page.dart';
import 'package:zorvian/features/accounting/pages/comparative_reports_page.dart';
import 'package:zorvian/features/accounting/pages/equity_changes_page.dart';
import 'package:zorvian/features/accounting/pages/fiscal_years_page.dart';
import 'package:zorvian/features/accounting/pages/income_statement_page.dart';
import 'package:zorvian/features/accounting/pages/trial_balance_page.dart';
import 'package:zorvian/features/reports/pages/audit_logs_page.dart';
import 'package:zorvian/features/reports/pages/reports_page.dart';
import 'package:zorvian/features/treasury/pages/bank_collection_page.dart';
import 'package:zorvian/features/treasury/pages/bank_commission_page.dart';
import 'package:zorvian/features/treasury/pages/bank_deposit_page.dart';
import 'package:zorvian/features/treasury/pages/bank_transfer_page.dart';
import 'package:zorvian/features/treasury/pages/check_issuance_page.dart';
import 'package:zorvian/features/treasury/pages/treasury_dashboard_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

/// Página vacía para los endpoints secundarios.
const _page = {
  'items': [],
  'totalCount': 0,
  'page': 1,
  'pageSize': 50,
  'totalPages': 0,
};

/// Respuestas de contabilidad, tesorería y reportes.
///
/// Las páginas que se montan aquí no toleran listas con claves ausentes: los
/// `fromJson` leen `periodName`, `accountCode`… con fallback, pero las tablas
/// necesitan filas reales para pintar el layout que se está midiendo.
Map<String, Object?> _routes() => const {
      // ── Contabilidad ────────────────────────────────────────────────────
      'accounts': [
        {
          'id': 'a1',
          'code': '1.1.01',
          'name': 'Caja General',
          'type': 'asset',
          'normalSide': 'debit',
          'level': 3,
          'isActive': true,
          'isSystem': true,
          'openingBalance': 0.0,
          'currentBalance': 12500.0,
        },
        {
          'id': 'a2',
          'code': '6.1.09',
          'name': 'Servicios y Honorarios Profesionales',
          'type': 'expense',
          'normalSide': 'debit',
          'level': 3,
          'isActive': true,
          'isSystem': false,
          'openingBalance': 0.0,
          'currentBalance': 8400.0,
        },
      ],
      'account-links': [
        {
          'id': 'al1',
          'transactionType': 'sale',
          'role': 'cash',
          'accountId': 'a1',
          'accountCode': '1.1.01',
          'accountName': 'Caja General',
        },
        {
          'id': 'al2',
          'transactionType': 'service_provider_payment',
          'role': 'services_expense',
          'accountId': 'a2',
          'accountCode': '6.1.09',
          'accountName': 'Servicios y Honorarios Profesionales',
        },
      ],
      'accounting-periods': [
        {
          'id': 'per1',
          'year': 2026,
          'month': 10,
          'name': 'Octubre 2026',
          'status': 'open',
        },
      ],
      'fiscal-years': [
        {
          'id': 'fy1',
          'year': 2026,
          'name': 'Año Fiscal 2026',
          'status': 'open',
          'startDate': '2026-01-01T00:00:00',
          'endDate': '2026-12-31T00:00:00',
        },
      ],
      'financial-reports/trial-balance/per1': {
        'periodName': 'Octubre 2026',
        'items': [
          {
            'accountCode': '1.1.01',
            'accountName': 'Caja General',
            'accountType': 'asset',
            'openingBalance': 5000.0,
            'debitMovements': 3000.0,
            'creditMovements': 500.0,
            'endingBalance': 7500.0,
          },
          {
            'accountCode': '6.1.09',
            'accountName': 'Servicios y Honorarios Profesionales',
            'accountType': 'expense',
            'openingBalance': 0.0,
            'debitMovements': 8400.0,
            'creditMovements': 0.0,
            'endingBalance': 8400.0,
          },
        ],
      },
      'financial-reports/income-statement/per1': {
        'periodName': 'Octubre 2026',
        'totalIncome': 50000.0,
        'totalCost': 20000.0,
        'grossProfit': 30000.0,
        'totalExpenses': 12000.0,
        'netIncome': 18000.0,
      },
      // ── Tesorería ───────────────────────────────────────────────────────
      'treasury/dashboard-summary': {
        'totalBankBalance': 125000.0,
        'pendingDeposits': 3,
        'outstandingChecks': 2,
        'pendingReconciliations': 1,
      },
      'bank-accounts': [
        {
          'id': 'ba1',
          'bankName': 'Banco de Prueba del Norte',
          'accountNumber': '001-234567-0001',
          'currency': 'NIO',
          'balance': 125000.0,
          'isActive': true,
        },
      ],
      'cost-centers': [
        {'id': 'cc1', 'name': 'Administración', 'isActive': true},
      ],
      'clients': {
        'items': [
          {'id': 'c1', 'name': 'Cliente de Prueba'},
        ],
        'totalCount': 1,
        'page': 1,
        'pageSize': 200,
        'totalPages': 1,
      },
      'suppliers': {
        'items': [
          {'id': 'sup1', 'name': 'Proveedor de Prueba'},
        ],
        'totalCount': 1,
        'page': 1,
        'pageSize': 200,
        'totalPages': 1,
      },
      'employees': {
        'items': [
          {'id': 'e1', 'firstName': 'Juan', 'lastName': 'Pérez'},
        ],
        'totalCount': 1,
        'page': 1,
        'pageSize': 200,
        'totalPages': 1,
      },
      'sales': {
        'items': [
          {
            'id': 's1',
            'invoiceNumber': 'F-0001',
            'total': 1150.0,
            'balance': 1150.0,
          },
        ],
        'totalCount': 1,
        'page': 1,
        'pageSize': 200,
        'totalPages': 1,
      },
      // ── Reportes ────────────────────────────────────────────────────────
      'audit/logs': {'data': [], 'totalCount': 0, 'page': 1, 'pageSize': 20},
    };

/// Contabilidad, tesorería y reportes a 360/390/430 px.
void main() {
  Future<void> mount(WidgetTester tester, Widget page) async {
    final api = FakeApi.routes(_routes(), fallback: _page);

    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [dioClientProvider.overrideWithValue(api.client)],
        child: MaterialApp(home: page),
      ),
      height: 1600,
    );
  }

  /// Selecciona el período de prueba y consulta el reporte.
  Future<void> selectPeriod(WidgetTester tester) async {
    await tester.tap(find.byType(DropdownButtonFormField<String>).first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Octubre 2026').last);
    await tester.pumpAndSettle();
  }

  testWidgets('catálogo de cuentas cabe en teléfono', (tester) async {
    await mount(tester, const ChartOfAccountsPage());

    expect(find.textContaining('Caja General'), findsWidgets);
    expect(find.textContaining('1.1.01'), findsWidgets);
  });

  testWidgets('vínculos contables caben en teléfono', (tester) async {
    await mount(tester, const AccountLinksPage());

    expect(find.textContaining('sale'), findsWidgets);
  });

  testWidgets('períodos contables caben en teléfono', (tester) async {
    await mount(tester, const AccountingPeriodsPage());

    expect(find.textContaining('Octubre 2026'), findsWidgets);
  });

  testWidgets('años fiscales caben en teléfono', (tester) async {
    await mount(tester, const FiscalYearsPage());

    expect(find.textContaining('Año Fiscal 2026'), findsWidgets);
  });

  testWidgets('balance de prueba con datos cabe en teléfono', (tester) async {
    await mount(tester, const TrialBalancePage());

    await selectPeriod(tester);
    await tester.tap(find.text('Consultar'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Caja General'), findsWidgets);
  });

  testWidgets('estado de resultados con datos cabe en teléfono', (tester) async {
    await mount(tester, const IncomeStatementPage());

    await selectPeriod(tester);
    await tester.tap(find.text('Consultar'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Utilidad Neta'), findsWidgets);
  });

  testWidgets('cambios en el patrimonio caben en teléfono', (tester) async {
    await mount(tester, const EquityChangesPage());

    expect(find.textContaining('período'), findsWidgets);
  });

  testWidgets('reportes comparativos caben en teléfono', (tester) async {
    await mount(tester, const ComparativeReportsPage());

    expect(find.textContaining('período'), findsWidgets);
  });

  testWidgets('dashboard de tesorería cabe en teléfono', (tester) async {
    await mount(tester, const TreasuryDashboardPage());

    expect(find.text('Resumen Financiero'), findsOneWidget);
    expect(find.text('Operaciones Rápidas'), findsOneWidget);
  });

  testWidgets('depósito bancario cabe en teléfono', (tester) async {
    await mount(tester, const BankDepositPage());

    expect(find.text('Cuenta Bancaria'), findsOneWidget);
  });

  testWidgets('comisión bancaria cabe en teléfono', (tester) async {
    await mount(tester, const BankCommissionPage());

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('transferencia bancaria cabe en teléfono', (tester) async {
    await mount(tester, const BankTransferPage());

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('cobranza bancaria cabe en teléfono', (tester) async {
    await mount(tester, const BankCollectionPage());

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('emisión de cheques cabe en teléfono', (tester) async {
    await mount(tester, const CheckIssuancePage());

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('centro de reportes cabe en teléfono', (tester) async {
    await mount(tester, const ReportsPage());

    expect(find.text('Generar Reporte'), findsOneWidget);
    expect(find.text('Vacaciones'), findsOneWidget);
  });

  testWidgets('bitácora de auditoría cabe en teléfono', (tester) async {
    await mount(tester, const AuditLogsPage());

    expect(find.byType(DropdownButtonFormField<String>), findsNWidgets(2));
  });
}
