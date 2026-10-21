import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/offline/connectivity_monitor.dart';
import 'package:zorvian/core/offline/sync_engine.dart';
import 'package:zorvian/features/bi/models/bi_models.dart';
import 'package:zorvian/features/bi/pages/commercial_dashboard_page.dart';
import 'package:zorvian/features/bi/providers/bi_provider.dart';
import 'package:zorvian/features/bi/pages/executive_dashboard_page.dart' as bi;
import 'package:zorvian/features/credits/pages/overdue_dashboard_page.dart';
import 'package:zorvian/features/dashboard/mobile_dashboard_page.dart';
import 'package:zorvian/features/dashboard/pages/absence_calendar_page.dart';
import 'package:zorvian/features/dashboard_v2/dashboard_v2_page.dart';
import 'package:zorvian/features/executive_dashboard/executive_dashboard_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_dashboard_page.dart';
import 'package:zorvian/features/products/pages/inventory_dashboard_page.dart';
import 'package:zorvian/features/products/providers/product_provider.dart';

import 'helpers/fake_api.dart';
import 'helpers/offline_db.dart';
import 'helpers/viewport.dart';

/// Barrido de dashboards y paneles a 360/390/430 px.
///
/// Las páginas con `AutoRefreshMixin` (los BI) se montan con un alto grande
/// —con el alto real solo se construirían las primeras secciones— y se
/// desmontan al final para cancelar el `Timer.periodic`.
///
/// Las de red usan `FakeApi`; las offline-first (`InventoryDashboardPage`)
/// usan el `AppDatabase` en memoria y una conectividad fija en `offline`.

// ---------------------------------------------------------------------------
// Fixtures de red
// ---------------------------------------------------------------------------

Map<String, Object?> _dashboardSummary() => {
      'kpis': {
        'totalEmployees': 42,
        'activeEmployees': 38,
        'inactiveEmployees': 4,
        'activeEmployeesTrend': 2.5,
        'pendingRequestsTrend': -1.25,
        'pendingVacationRequests': 3,
        'pendingPermissionRequests': 5,
        'birthdaysThisMonth': 2,
        'workAnniversariesThisMonth': 1,
        'employeesByDepartment': [
          {'departmentName': 'Administración y finanzas', 'count': 8},
          {'departmentName': 'Bodega central de distribución', 'count': 12},
        ],
      },
      'calendarEvents': [
        {
          'employeeId': 'e1',
          'employeeName': 'José Ramón Castillo Vega',
          'employeeCode': 'EMP-0042',
          'type': 'vacation',
          'startDate': '2026-10-20',
          'endDate': '2026-10-24',
          'status': 'approved',
        },
      ],
      'recentRequests': [
        {
          'id': 'r1',
          'requestType': 'permission',
          'employeeName': 'María Fernanda López',
          'status': 'pending',
          'description': 'Cita médica en el INSS',
          'createdAt': '2026-10-20T09:00:00',
        },
      ],
    };

Map<String, Object?> _dashboardExecutive() => {
      'commercial': {
        'todaySales': 12500.5,
        'monthSales': 340000.0,
        'averageTicket': 620.0,
        'todaySalesCount': 18,
      },
      'credits': {
        'activeCredits': 12,
        'overdueCredits': 3,
        'monthlyRecovery': 98000.0,
        'totalPortfolio': 450000.0,
      },
      'inventory': {
        'outOfStockProducts': 2,
        'lowStockProducts': 7,
        'totalProducts': 240,
        'topSelling': [
          {'productName': 'Agua Purificada 600 ml', 'totalSold': 120},
          {'productName': 'Refresco de cola 3 litros', 'totalSold': 84},
        ],
      },
      'cash': {
        'todayIncome': 18000.0,
        'todayExpense': 7500.0,
        'openRegisters': 2,
      },
      'humanResources': {
        'activeEmployees': 38,
        'pendingVacations': 3,
        'activePermissions': 5,
      },
    };

Map<String, Object?> _overdueDashboard() => {
      'totalOverdueCredits': 6,
      'totalActiveCredits': 24,
      'totalPortfolio': 1250000.0,
      'totalOverdueBalance': 185400.0,
      'recoveryRate': 0.82,
      'agingBuckets': [
        {
          'label': '1-30 días',
          'creditCount': 3,
          'installmentCount': 5,
          'totalBalance': 92000.0,
          'totalAmount': 100000.0,
        },
        {
          'label': '31-60 días',
          'creditCount': 2,
          'installmentCount': 3,
          'totalBalance': 63400.0,
          'totalAmount': 70000.0,
        },
        {
          'label': 'Más de 60 días',
          'creditCount': 1,
          'installmentCount': 2,
          'totalBalance': 30000.0,
          'totalAmount': 34000.0,
        },
      ],
      'criticalOverdue': [
        {
          'id': 'i1',
          'installmentNumber': 4,
          'dueDate': '2026-08-15',
          'amount': 15400.0,
          'balance': 15400.0,
          'daysOverdue': 67,
          'status': 'overdue',
        },
      ],
    };

Map<String, Object?> _fleetDashboard() => {
      'totalVehicles': 18,
      'activeVehicles': 15,
      'inMaintenance': 3,
      'availableDrivers': 12,
      'activeRoutes': 4,
      'pendingDeliveries': 7,
      'tripsToday': 9,
      'expiringDocuments': 2,
      'overdueMaintenance': 1,
      'alerts': [
        {
          'type': 'maintenance',
          'severity': 'critical',
          'message': 'Mantenimiento preventivo vencido hace 12 días en el '
              'vehículo MZ-4210 (camión de reparto)',
          'entityId': 'v1',
          'entityType': 'Vehicle',
        },
      ],
    };

List<Map<String, Object?>> _vacationCalendar() {
  final now = DateTime.now();
  final start = DateTime(now.year, now.month, 3);
  final end = DateTime(now.year, now.month, 8);
  String iso(DateTime d) => d.toIso8601String().substring(0, 10);
  return [
    {
      'employeeId': 'e1',
      'employeeName': 'José Ramón Castillo Vega',
      'employeeCode': 'EMP-0042',
      'type': 'vacation',
      'startDate': iso(start),
      'endDate': iso(end),
      'status': 'approved',
    },
    {
      'employeeId': 'e2',
      'employeeName': 'María Fernanda López',
      'employeeCode': 'EMP-0043',
      'type': 'permission',
      'startDate': iso(end),
      'endDate': iso(end),
      'status': 'pending',
    },
  ];
}

// ---------------------------------------------------------------------------
// Fixtures BI (offline)
// ---------------------------------------------------------------------------

BiExecutive _executive() => BiExecutive(
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
      alerts: [
        BiAlert(
          type: 'stock',
          message: 'Producto Agua Purificada 600ml con stock por debajo del '
              'mínimo configurado en la bodega principal',
          severity: 'critical',
        ),
      ],
    );

List<BiMonthSales> _salesTrend() => [
      for (var i = 0; i < 12; i++)
        BiMonthSales(
          year: 2026,
          month: i + 1,
          total: 120000 + i * 15000,
          count: 40 + i,
          averageTicket: 850,
          totalCost: 78000,
          margin: 0.35,
        ),
    ];

/// Payload de `bi/sales-trend` (la página comercial pide todo por red).
Map<String, Object?> _salesTrendPayload() => {
      'monthly': [
        for (final m in _salesTrend())
          {
            'year': m.year,
            'month': m.month,
            'total': m.total,
            'count': m.count,
            'averageTicket': m.averageTicket,
            'totalCost': m.totalCost,
            'margin': m.margin,
          },
      ],
    };

BranchComparison _branchComparison() => BranchComparison(
      branches: [
        BranchComparisonItem(
          branchId: 'b1',
          branchName: 'Sucursal Central Managua',
          monthSales: 210000,
          salesCount: 120,
          monthRecovery: 180000,
          portfolio: 340000,
          activeCredits: 18,
          overdueCredits: 2,
          inventoryValue: 560000,
        ),
        BranchComparisonItem(
          branchId: 'b2',
          branchName: 'Sucursal León',
          monthSales: 135678,
          salesCount: 74,
          monthRecovery: 98000,
          portfolio: 110000,
          activeCredits: 6,
          overdueCredits: 1,
          inventoryValue: 330000,
        ),
      ],
      totalMonthSales: 345678,
      totalSalesCount: 194,
      totalMonthRecovery: 278000,
      totalPortfolio: 450000,
      totalActiveCredits: 24,
      totalOverdueCredits: 3,
      totalInventoryValue: 890000,
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

class _BiNotifierMock extends BiNotifier {
  @override
  Future<BiExecutive> build() async => _executive();
}

class _OfflineConnectivity extends ConnectivityMonitor {
  @override
  ConnectivityStatus build() => ConnectivityStatus.offline;
}

// ---------------------------------------------------------------------------
// Harness
// ---------------------------------------------------------------------------

/// Monta la página en los tres teléfonos con un alto grande para que el
/// `ListView` construya todas las secciones, y la desmonta para cancelar el
/// `Timer.periodic` que dejan los dashboards con `AutoRefreshMixin`.
Future<void> _pumpPage(WidgetTester tester, Widget app) async {
  await pumpAcrossPhones(tester, (_) => app, height: 4000);
}

Future<void> _disposePage(WidgetTester tester) =>
    tester.pumpWidget(const SizedBox());

Widget _withApi(Map<String, Object?> routes, Widget child) {
  final api = FakeApi.routes(routes);
  return ProviderScope(
    overrides: [dioClientProvider.overrideWithValue(api.client)],
    child: MaterialApp(home: child),
  );
}

class _ProductBootstrap extends ConsumerStatefulWidget {
  const _ProductBootstrap(this.child);

  final Widget child;

  @override
  ConsumerState<_ProductBootstrap> createState() => _ProductBootstrapState();
}

class _ProductBootstrapState extends ConsumerState<_ProductBootstrap> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(productProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) => widget.child;
}

void main() {
  testWidgets('MobileDashboardPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _withApi(
        {'dashboard/summary': _dashboardSummary()},
        const Scaffold(body: MobileDashboardPage()),
      ),
    );

    expect(find.byType(MobileDashboardPage), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('DashboardV2Page cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      const ProviderScope(child: MaterialApp(home: DashboardV2Page())),
    );

    expect(find.byType(DashboardV2Page), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('ExecutiveDashboardPage (dashboard) cabe en teléfono',
      (tester) async {
    await _pumpPage(
      tester,
      _withApi(
        {'dashboard/executive': _dashboardExecutive()},
        const ExecutiveDashboardPage(),
      ),
    );

    expect(find.byType(ExecutiveDashboardPage), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('Dashboard ejecutivo BI cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      ProviderScope(
        overrides: [
          biProvider.overrideWith(_BiNotifierMock.new),
          biSalesTrendProvider.overrideWith((ref, months) => _salesTrend()),
          biBranchComparisonProvider.overrideWith((ref) => _branchComparison()),
          biArAgingProvider.overrideWith((ref) => _arAging()),
          biFinancialRatiosProvider.overrideWith((ref) => _ratios()),
        ],
        child: const MaterialApp(home: bi.ExecutiveDashboardPage()),
      ),
    );

    expect(find.byType(bi.ExecutiveDashboardPage), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('Dashboard comercial BI cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _withApi(
        {
          'bi/sales-trend': _salesTrendPayload(),
          // Secciones de IA del dashboard: sin respuesta quedarían en el
          // `CircularProgressIndicator` de carga y `pumpAndSettle` nunca
          // terminaría (la animación es infinita).
          'sales/predictions/next-week': [
            {
              'date': '2026-10-22',
              'dayOfWeek': 'Jueves',
              'predictedSales': 18200.0,
              'lowerBound': 15000.0,
              'upperBound': 21000.0,
            },
          ],
          'sales/predictions/next-month': {
            'totalPredicted': 512000.0,
            'dailyAverage': 16500.0,
            'predictions': [
              {
                'date': '2026-11-01',
                'dayOfWeek': 'Domingo',
                'predictedSales': 12000.0,
                'lowerBound': 9000.0,
                'upperBound': 15000.0,
              },
            ],
          },
          'sales/predictions/monthly-total': {
            'month': 10,
            'year': 2026,
            'salesSoFar': 345678.0,
            'predictedTotal': 480000.0,
            'projectedTotal': 492000.0,
            'remainingDays': 10,
          },
          'purchases/recommendations': {
            'totalProducts': 12,
            'criticalCount': 2,
            'warningCount': 4,
            'healthyCount': 6,
            'totalRecommendedCost': 84000.0,
            'recommendations': <Object>[],
          },
          'expense-classification/predict': {
            'description': 'Compra de combustible',
            'amount': 0.0,
            'suggestions': <Object>[],
          },
        },
        const Scaffold(body: CommercialDashboardPage()),
      ),
    );

    expect(find.byType(CommercialDashboardPage), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('OverdueDashboardPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _withApi(
        {'credits/overdue-dashboard': _overdueDashboard()},
        const OverdueDashboardPage(),
      ),
    );

    expect(find.byType(OverdueDashboardPage), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('InventoryDashboardPage cabe en teléfono', (tester) async {
    final db = memoryDb();
    addTearDown(db.close);
    await db.upsertProduct({
      'id': 'p1',
      'code': 'AGU-600',
      'name': 'Agua Purificada 600 ml',
      'categoryName': 'Bebidas',
      'sellingPrice': 18.0,
      'costPrice': 11.0,
      'stock': 4,
      'minStock': 24,
      'unitOfMeasure': 'pz',
      'isActive': true,
    });
    await db.upsertProduct({
      'id': 'p2',
      'code': 'REF-3000',
      'name': 'Refresco de cola 3 litros',
      'categoryName': 'Bebidas',
      'sellingPrice': 62.0,
      'costPrice': 44.0,
      'stock': 0,
      'minStock': 12,
      'unitOfMeasure': 'pz',
      'isActive': true,
    });

    await _pumpPage(
      tester,
      ProviderScope(
        overrides: [
          appDatabaseProvider.overrideWithValue(db),
          connectivityProvider.overrideWith(_OfflineConnectivity.new),
        ],
        child: const MaterialApp(
          home: _ProductBootstrap(
            Scaffold(body: InventoryDashboardPage()),
          ),
        ),
      ),
    );

    expect(find.byType(InventoryDashboardPage), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('FleetDashboardPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _withApi({'fleet/dashboard': _fleetDashboard()}, const FleetDashboardPage()),
    );

    expect(find.byType(FleetDashboardPage), findsOneWidget);
    await _disposePage(tester);
  });

  testWidgets('AbsenceCalendarPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _withApi(
        {'dashboard/vacation-calendar': _vacationCalendar()},
        const AbsenceCalendarPage(),
      ),
    );

    expect(find.byType(AbsenceCalendarPage), findsOneWidget);
    await _disposePage(tester);
  });
}
