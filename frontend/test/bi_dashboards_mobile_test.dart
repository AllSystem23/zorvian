import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/core/widgets/bi/anomaly_detection_section.dart';
import 'package:zorvian/features/bi/models/bi_models.dart';
import 'package:zorvian/features/bi/pages/financial_dashboard_page.dart';
import 'package:zorvian/features/bi/pages/operational_dashboard_page.dart';
import 'package:zorvian/features/bi/providers/bi_provider.dart';

import 'helpers/viewport.dart';

/// Barrido de los dashboards financiero y operacional a 360/390/430 px.
///
/// Los providers se sustituyen por datos fijos: el objetivo es el layout, no
/// la red. Cada página trae `AutoRefreshMixin` (Timer.periodic de 60 s), así
/// que el harness desmonta el árbol al final para cancelar el timer.

BiExecutive _executive({bool withAlerts = true}) => BiExecutive(
      sales: BiSalesKpi(
        todaySales: 12345,
        yesterdaySales: 10000,
        salesChangePercent: 0.23,
        monthSales: 345678,
        monthSalesChangePercent: -0.05,
        averageTicket: 850,
        todaySalesCount: 15,
        weeklyTrend: const [1, 2, 3, 4, 5, 6, 7],
      ),
      credits: BiCreditKpi(
        activeCredits: 12,
        overdueCredits: 3,
        monthlyRecovery: 56000,
        totalPortfolio: 220000,
        collectionRate: 0.82,
        dsoDays: 41,
      ),
      inventory: BiInventoryKpi(
        outOfStockProducts: 2,
        lowStockProducts: 7,
        totalProducts: 240,
        totalStockValue: 890000,
        turnoverRate: 3.4,
        topSelling: const [],
      ),
      cash: BiCashKpi(
        todayIncome: 15000,
        todayExpense: 4000,
        netCashFlow: 11000,
        openRegisters: 2,
      ),
      hr: BiHrKpi(
        activeEmployees: 42,
        totalEmployees: 45,
        pendingRequests: 3,
        payrollTotal: 560000,
        avgCostPerEmployee: 13333,
      ),
      alerts: withAlerts
          ? [
              BiAlert(
                type: 'stock',
                message: 'Producto Agua Purificada 600ml con stock por debajo del '
                    'mínimo configurado en la bodega principal',
                severity: 'critical',
              ),
            ]
          : const [],
    );

BiArAging _arAging() => BiArAging(
      current: 120000,
      days30: 45000,
      days60: 22000,
      days90: 9000,
      days90Plus: 4000,
      totalOverdue: 200000,
      totalPortfolio: 320000,
      overduePercent: 0.62,
      byClient: const [],
    );

BiApAging _apAging() => BiApAging(
      current: 80000,
      days30: 30000,
      days60: 12000,
      days90: 5000,
      days90Plus: 1000,
      totalOverdue: 128000,
      totalPayable: 200000,
      overduePercent: 0.64,
      bySupplier: const [],
    );

BiFinancialRatios _ratios() => BiFinancialRatios(
      currentRatio: 1.8,
      quickRatio: 1.1,
      debtRatio: 0.42,
      debtToEquity: 0.7,
      grossMargin: 0.38,
      netMargin: 0.12,
      operatingMargin: 0.18,
      roa: 0.09,
      roe: 0.21,
      workingCapital: 250000,
      breakEvenPoint: 410000,
    );

BiIncomeStatement _statement(String period) => BiIncomeStatement(
      periodName: period,
      totalIncome: 900000,
      totalCost: 560000,
      grossProfit: 340000,
      totalExpenses: 220000,
      netIncome: 120000,
    );

BiComparativeIncome _comparative() => BiComparativeIncome(
      current: _statement('Octubre 2026'),
      previous: _statement('Septiembre 2026'),
      changes: [
        BiLineChange(
          name: 'Ingresos por ventas de contado y crédito',
          currentValue: 900000,
          previousValue: 850000,
          changePercent: 0.0588,
        ),
        BiLineChange(
          name: 'Costo de ventas',
          currentValue: 560000,
          previousValue: 540000,
          changePercent: 0.037,
        ),
      ],
    );

BiCashFlow _cashFlow() => BiCashFlow(
      operating: [BiCashFlowItem(description: 'Cobros', amount: 90000, type: 'operating')],
      investing: [BiCashFlowItem(description: 'Equipos', amount: -30000, type: 'investing')],
      financing: [BiCashFlowItem(description: 'Préstamo', amount: 20000, type: 'financing')],
      netIncrease: 80000,
      beginningCash: 100000,
      endingCash: 180000,
    );

BiInventorySummary _inventory() => BiInventorySummary(
      totalValue: 890000,
      totalProducts: 240,
      lowStockCount: 7,
      outOfStockCount: 2,
      turnoverRate: 3.4,
      deadStockValue: 45000,
      byCategory: [
        BiCategoryInventory(
          categoryName: 'Abarrotes y productos de consumo masivo',
          count: 120,
          totalCost: 300000,
          totalValue: 450000,
        ),
        BiCategoryInventory(
          categoryName: 'Bebidas',
          count: 60,
          totalCost: 150000,
          totalValue: 240000,
        ),
      ],
    );

BiPayrollSummary _payroll() => BiPayrollSummary(
      byDepartment: [
        BiPayrollDept(department: 'Administración y finanzas', amount: 220000, employeeCount: 8),
        BiPayrollDept(department: 'Ventas', amount: 180000, employeeCount: 14),
      ],
      trend: const [],
      totalCost: 560000,
      averagePerEmployee: 13333,
      overtimeTotal: 24000,
      employerCostTotal: 86000,
    );

AccountingAnomalyReport _anomalies() => AccountingAnomalyReport(
      totalEntriesAnalyzed: 128,
      anomalyCount: 0,
      anomalies: const [],
    );

AccountingAnomalyReport _anomaliesWithItems() => AccountingAnomalyReport(
      totalEntriesAnalyzed: 128,
      anomalyCount: 1,
      anomalies: [
        AccountingAnomaly(
          accountingEntryId: '1',
          entryNumber: 'AS-20261020-000012345',
          description: 'Venta',
          referenceType: 'Sale',
          anomalyType: 'unbalanced',
          detail: 'El asiento no cuadra: el débito supera al crédito en 1,250.00 '
              'por un impuesto mal aplicado en la línea de servicios.',
          severity: 'critical',
          entryDate: '2026-10-20T10:00:00',
          totalDebit: 5000,
          totalCredit: 3750,
        ),
      ],
    );

class _BiNotifierMock extends BiNotifier {
  @override
  Future<BiExecutive> build() async => _executive();
}

Widget _financialDashboard() => ProviderScope(
      overrides: [
        biProvider.overrideWith(_BiNotifierMock.new),
        biFinancialRatiosProvider.overrideWith((ref) => _ratios()),
        biComparativeIncomeProvider.overrideWith((ref) => _comparative()),
        biCashFlowProvider.overrideWith((ref) => _cashFlow()),
        biArAgingProvider.overrideWith((ref) => _arAging()),
        biApAgingProvider.overrideWith((ref) => _apAging()),
        accountingAnomaliesProvider.overrideWith((ref) => _anomalies()),
      ],
      child: const MaterialApp(home: FinancialDashboardPage()),
    );

Widget _operationalDashboard() => ProviderScope(
      overrides: [
        biInventorySummaryProvider.overrideWith((ref) => _inventory()),
        biPayrollSummaryProvider.overrideWith((ref) => _payroll()),
      ],
      child: const MaterialApp(home: OperationalDashboardPage()),
    );

/// Monta la página en los tres teléfonos y luego la desmonta para cancelar el
/// `Timer.periodic` del `AutoRefreshMixin`.
///
/// Se usa un alto grande para que el `ListView` construya **todas** las
/// secciones: con el alto real de un teléfono solo se montan las primeras y los
/// overflows del final de la página pasarían desapercibidos.
Future<void> _pumpPage(WidgetTester tester, Widget app) async {
  await pumpAcrossPhones(tester, (_) => app, height: 4000);
}

Future<void> _disposePage(WidgetTester tester) =>
    tester.pumpWidget(const SizedBox());

Future<void> _pumpAnomalies(
  WidgetTester tester,
  AccountingAnomalyReport report,
) async {
  await pumpAcrossPhones(
    tester,
    (_) => ProviderScope(
      overrides: [
        accountingAnomaliesProvider.overrideWith((ref) => report),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: ListView(
            padding: const EdgeInsets.all(16),
            children: const [AnomalyDetectionSection()],
          ),
        ),
      ),
    ),
  );
}

void main() {
  testWidgets('FinancialDashboardPage cabe en teléfono', (tester) async {
    await _pumpPage(tester, _financialDashboard());

    expect(find.byType(FinancialDashboardPage), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('OperationalDashboardPage cabe en teléfono', (tester) async {
    await _pumpPage(tester, _operationalDashboard());

    expect(find.byType(OperationalDashboardPage), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('AnomalyDetectionSection cabe en teléfono', (tester) async {
    await _pumpAnomalies(tester, _anomalies());

    expect(find.textContaining('No se detectaron anomalías'), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('AnomalyDetectionSection con anomalías largas cabe en teléfono',
      (tester) async {
    await _pumpAnomalies(tester, _anomaliesWithItems());

    expect(find.text('AS-20261020-000012345'), findsOneWidget);
    await _disposePage(tester);
  });
}
