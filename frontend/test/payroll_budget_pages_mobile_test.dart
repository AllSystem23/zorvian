import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart'
    show AuthNotifier, AuthState, AuthStatus, authProvider, dioClientProvider;
import 'package:zorvian/core/offline/sync_engine.dart' show appDatabaseProvider;
import 'package:zorvian/features/budgets/pages/budget_list_page.dart';
import 'package:zorvian/features/budgets/pages/budget_vs_actual_page.dart';
import 'package:zorvian/features/payroll/pages/deduction_types_page.dart';
import 'package:zorvian/features/payroll/pages/payroll_page.dart';
import 'package:zorvian/features/payroll/pages/payroll_periods_page.dart';
import 'package:zorvian/features/payroll/pages/salaries_page.dart';
import 'package:zorvian/features/webhooks/pages/webhook_form_page.dart';
import 'package:zorvian/features/webhooks/pages/webhook_list_page.dart';
import 'package:zorvian/features/webhooks/pages/webhook_logs_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/offline_db.dart';
import 'helpers/viewport.dart';

/// Página vacía que devuelve la API para los endpoints secundarios.
const _page = {
  'items': [],
  'totalCount': 0,
  'page': 1,
  'pageSize': 50,
  'totalPages': 0,
};

/// Rutas de nómina, presupuestos y webhooks con datos reales.
///
/// Estas páginas castan la respuesta a `List` / `Map` sin defensas: con el
/// fallback paginado caen en su estado de error y el test solo validaría un
/// mensaje. Los campos replican los modelos (`BudgetItem`, `WebhookItem`…).
Map<String, Object?> _routes() => const {
      'employees': {
        'items': [],
        'total': 12,
        'page': 1,
        'pageSize': 1,
        'totalPages': 1,
      },
      'payroll/runs': [
        {
          'id': 'run1',
          'periodName': 'Octubre 2026 - Quincena 2',
          'status': 'draft',
          'total': 50000.0,
          'totalNetPay': 41500.0,
        },
      ],
      'payroll/periods': [
        {
          'id': 'per1',
          'name': 'Octubre 2026',
          'startDate': '2026-10-01',
          'endDate': '2026-10-31',
          'status': 'open',
          'year': 2026,
          'month': 10,
          'periodNumber': 1,
        },
      ],
      'payroll/salaries': [
        {
          'id': 'sal1',
          'employeeName': 'Juan Pérez',
          'baseSalary': 25000.0,
          'salaryType': 'monthly',
          'isActive': true,
        },
      ],
      'payroll/deduction-types': [
        {
          'id': 'dt1',
          'code': 'AFP',
          'name': 'AFP Laboral',
          'calculationMethod': 'percentage',
          'rate': 7.0,
        },
      ],
      'budgets': [
        {
          'id': 'b1',
          'year': 2026,
          'month': 10,
          'accountId': 'a1',
          'accountName': 'Gastos de administración',
          'accountCode': '6.2.01',
          'budgetedAmount': 10000.0,
        },
      ],
      'financial-reports/budget-vs-actual': {
        'items': [
          {
            'budgetId': 'b1',
            'year': 2026,
            'month': 10,
            'accountId': 'a1',
            'accountCode': '6.2.01',
            'accountName': 'Gastos de administración',
            'budgetedAmount': 10000.0,
            'actualAmount': 9400.0,
            'variance': 600.0,
            'variancePercent': 6.0,
          },
        ],
      },
      'webhooks/wh1/logs': [
        {
          'attempt': 1,
          'executedAt': '2026-10-19T10:00:00',
          'httpStatusCode': 200,
          'success': true,
          'maxRetries': 3,
        },
      ],
      'webhooks/wh1': {
        'id': 'wh1',
        'eventType': 'sale.created',
        'targetUrl': 'https://example.com/hooks/ventas',
        'description': 'Notifica cada venta al sistema contable',
        'isActive': true,
        'maxRetries': 3,
        'retryIntervalSeconds': 60,
        'secret': 'whsec_abcdef123456',
      },
      'webhooks': [
        {
          'id': 'wh1',
          'eventType': 'sale.created',
          'targetUrl': 'https://example.com/hooks/ventas',
          'description': 'Notifica cada venta al sistema contable',
          'isActive': true,
          'maxRetries': 3,
          'retryIntervalSeconds': 60,
          'secret': 'whsec_abcdef123456',
        },
      ],
    };

/// Auth con rol fijo (los enlaces de administración solo se pintan para
/// SuperAdmin/CompanyAdmin).
class _FixedAuthNotifier extends AuthNotifier {
  final AuthState _fixedState;
  _FixedAuthNotifier(this._fixedState);

  @override
  AuthState build() => _fixedState;
}

/// Nómina, presupuestos y webhooks a 360/390/430 px.
void main() {
  Future<void> mount(WidgetTester tester, Widget page) async {
    final api = FakeApi.routes(_routes(), fallback: _page);
    final db = memoryDb();
    addTearDown(db.close);

    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [
          dioClientProvider.overrideWithValue(api.client),
          appDatabaseProvider.overrideWithValue(db),
        ],
        child: MaterialApp(home: page),
      ),
      height: 1600,
    );
  }

  testWidgets('página de nómina cabe en teléfono', (tester) async {
    await mount(tester, const PayrollPage());

    expect(find.text('Corridas de Nómina'), findsOneWidget);
  });

  testWidgets('nómina con enlaces de administración cabe en teléfono', (tester) async {
    final api = FakeApi.routes(_routes(), fallback: _page);
    final db = memoryDb();
    addTearDown(db.close);

    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [
          authProvider.overrideWith(() => _FixedAuthNotifier(const AuthState(
            status: AuthStatus.authenticated,
            role: 'SuperAdmin',
          ))),
          dioClientProvider.overrideWithValue(api.client),
          appDatabaseProvider.overrideWithValue(db),
        ],
        child: const MaterialApp(home: PayrollPage()),
      ),
      height: 1600,
    );

    expect(find.text('Tipos de Deducción'), findsOneWidget);
    // 'Períodos' aparece en la tarjeta resumen y en el enlace de administración.
    expect(find.text('Períodos'), findsNWidgets(2));
  });

  testWidgets('períodos de nómina caben en teléfono', (tester) async {
    await mount(tester, const PayrollPeriodsPage());

    expect(find.text('Octubre 2026'), findsOneWidget);
    expect(find.text('Abierto'), findsOneWidget);
  });

  testWidgets('salarios caben en teléfono', (tester) async {
    await mount(tester, const SalariesPage());

    expect(find.text('Juan Pérez'), findsOneWidget);
    expect(find.text('Activo'), findsOneWidget);
  });

  testWidgets('tipos de deducción caben en teléfono', (tester) async {
    await mount(tester, const DeductionTypesPage());

    expect(find.text('AFP Laboral'), findsOneWidget);
    expect(find.textContaining('7.0%'), findsOneWidget);
  });

  testWidgets('listado de presupuestos cabe en teléfono', (tester) async {
    await mount(tester, const BudgetListPage());

    expect(find.textContaining('Gastos de administración'), findsWidgets);
    expect(find.textContaining('2026/10'), findsWidgets);
  });

  testWidgets('presupuesto vs real cabe en teléfono', (tester) async {
    await mount(tester, const BudgetVsActualPage());

    expect(find.textContaining('Gastos de administración'), findsWidgets);
    expect(find.textContaining('10000.00'), findsWidgets);
  });

  testWidgets('listado de webhooks cabe en teléfono', (tester) async {
    await mount(tester, const WebhookListPage());

    expect(find.textContaining('Venta creada'), findsWidgets);
    expect(find.textContaining('example.com'), findsWidgets);
  });

  testWidgets('formulario de webhook cabe en teléfono', (tester) async {
    await mount(tester, const WebhookFormPage(webhookId: 'wh1'));

    // `DropdownButtonFormField` del tipo de evento + switch Activo.
    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
    expect(find.text('Activo'), findsOneWidget);
  });

  testWidgets('logs de webhook caben en teléfono', (tester) async {
    await mount(tester, const WebhookLogsPage(webhookId: 'wh1'));

    expect(find.textContaining('Intento 1 de 3'), findsWidgets);
  });
}
