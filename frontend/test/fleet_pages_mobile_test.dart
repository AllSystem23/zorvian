import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/fleet/pages/fleet_alerts_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_predictive_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_reports_page.dart';
import 'package:zorvian/features/fleet/pages/fleet_tracking_page.dart';
import 'package:zorvian/features/fleet/providers/fleet_alerts_provider.dart';
import 'package:zorvian/features/fleet/providers/fleet_predictive_provider.dart';
import 'package:zorvian/features/fleet/providers/fleet_reports_provider.dart';
import 'package:zorvian/features/fleet/providers/fleet_tracking_provider.dart';

import 'helpers/viewport.dart';

// ── Fixtures ──

Map<String, dynamic> _alert({
  required String severity,
  required String title,
  String category = 'Mantenimiento',
}) =>
    {
      'id': title,
      'severity': severity,
      'category': category,
      'title': title,
      'message':
          'El vehículo ABC-123 superó el intervalo de mantenimiento por 1.480 km. Programe la revisión en el taller autorizado.',
      'createdAt': '2026-10-20T08:15:00',
    };

FleetAlertState _alertsState() => FleetAlertState(
      summary: const {
        'activeAlerts': 12,
        'criticalAlerts': 3,
        'warningAlerts': 5,
        'infoAlerts': 4,
        'unacknowledgedAlerts': 2,
      },
      activeAlerts: [
        _alert(
          severity: 'critical',
          title: 'Mantenimiento vencido — Camión ABC-123',
        ),
        _alert(
          severity: 'warning',
          title: 'Documento por vencer: póliza de seguro',
          category: 'Documentación',
        ),
        _alert(
          severity: 'info',
          title: 'Consumo de combustible fuera de rango',
          category: 'Combustible',
        ),
      ],
    );

FleetPredictiveState _predictiveState() => const FleetPredictiveState(
      maintenanceSummary: {
        'totalVehicles': 24,
        'vehiclesOverdue': 3,
        'vehiclesDueSoon': 5,
        'avgRiskScore': 42,
      },
      fuelSummary: {
        'totalRefillsAnalyzed': 148,
        'anomaliesDetected': 6,
        'highSeverity': 2,
        'estimatedMonthlyWaste': 12450.75,
      },
      forecasts: [
        {
          'vehicleId': 'v1',
          'plate': 'ABC-123',
          'brandModel': 'Isuzu NPR 2021 — carga pesada',
          'riskLevel': 'Critical',
          'riskScore': 87,
          'components': [
            {
              'componentName': 'Aceite de motor',
              'status': 'Overdue',
              'daysUntilDue': -12,
              'kmUntilDue': -1480,
            },
            {
              'componentName': 'Filtro de aire',
              'status': 'DueSoon',
              'daysUntilDue': 6,
              'kmUntilDue': 420,
            },
          ],
        },
        {
          'vehicleId': 'v2',
          'plate': 'XYZ-987',
          'brandModel': 'Toyota Hilux 2022',
          'riskLevel': 'Low',
          'riskScore': 12,
          'components': [],
        },
      ],
      fuelAnomalies: [
        {
          'id': 'a1',
          'plate': 'ABC-123',
          'anomalyType': 'HighConsumption',
          'severity': 'High',
          'description':
              'Consumo 38% por encima del promedio histórico del vehículo en los últimos 30 días.',
          'liters': 92.5,
          'totalCost': 14820.40,
          'deviationPercent': 38,
        },
        {
          'id': 'a2',
          'plate': 'XYZ-987',
          'anomalyType': 'LowEfficiency',
          'severity': 'Medium',
          'description': 'Rendimiento por debajo del rango esperado.',
          'liters': 45.0,
          'totalCost': 7200.00,
          'deviationPercent': 14,
        },
      ],
      fuelTrends: [
        {
          'plate': 'ABC-123',
          'avgConsumptionPer100Km': 28.4,
          'minConsumptionPer100Km': 22.1,
          'maxConsumptionPer100Km': 34.8,
          'stdDeviation': 3.62,
          'currentTrend': 2.4,
          'sampleCount': 37,
        },
      ],
    );

const _trackingDetail = {
  'id': 'd1',
  'code': 'ENT-20261020-0042',
  'status': 'InTransit',
  'deliveryAddress':
      'Km 12 Carretera Norte, frente a la gasolinera UNO, Managua, Nicaragua',
  'clientName': 'Comercial Brizuela Romero, S.A.',
  'scheduledDate': '2026-10-20T08:30:00',
};

const _trackingDeliveries = [
  {
    'id': 'd1',
    'code': 'ENT-20261020-0042',
    'status': 'InTransit',
    'deliveryAddress': 'Km 12 Carretera Norte, Managua',
    'clientName': 'Comercial Brizuela Romero, S.A.',
    'vehiclePlate': 'ABC-123',
  },
  {
    'id': 'd2',
    'code': 'ENT-20261019-0038',
    'status': 'Delivered',
    'deliveryAddress': 'Rotonda Rubén Darío, Managua',
    'clientName': 'Distribuidora La Unión',
    'vehiclePlate': 'XYZ-987',
  },
];

const _trackingTimeline = [
  {
    'status': 'Created',
    'timestamp': '2026-10-20T07:00:00',
    'notes': 'Entrega programada por el área comercial.',
  },
  {
    'status': 'InTransit',
    'timestamp': '2026-10-20T08:30:00',
    'notes':
        'El conductor reporta salida del centro de distribución con 12 bultos.',
  },
];

const _fleetKpiData = {
  'totalVehicles': 24,
  'activeVehicles': 19,
  'availableVehicles': 11,
  'inMaintenanceVehicles': 3,
  'outOfServiceVehicles': 2,
  'fleetAvailabilityRate': 79.2,
  'averageCostPerKm': 1.87,
  'averageFuelEfficiency': 8.4,
  'totalDeliveries': 312,
  'completedDeliveries': 289,
  'onTimeDeliveryRate': 92.6,
  'expiringDocuments': 4,
  'overdueMaintenance': 3,
  'openWorkOrders': 5,
};

const _costSummaryData = {
  'grandTotal': 48250.75,
  'categories': [
    {'categoryName': 'Combustible y lubricantes', 'totalAmount': 22000.0},
    {'categoryName': 'Mantenimiento preventivo', 'totalAmount': 14250.75},
    {'categoryName': 'Peajes y viáticos', 'totalAmount': 12000.0},
  ],
};

const _costTrendData = {
  'trends': [
    {
      'monthLabel': 'Ene',
      'fuelCost': 1200.50,
      'maintenanceCost': 800.00,
      'expenseCost': 400.00,
      'totalCost': 2400.50,
    },
    {
      'monthLabel': 'Feb',
      'fuelCost': 18250.75,
      'maintenanceCost': 9800.25,
      'expenseCost': 3400.00,
      'totalCost': 31451.00,
    },
    {
      'monthLabel': 'Mar',
      'fuelCost': 9400.00,
      'maintenanceCost': 4200.00,
      'expenseCost': 1800.00,
      'totalCost': 15400.00,
    },
  ],
};

const _fuelTrendData = {
  'grandTotalLiters': 4230.5,
  'grandTotalCost': 98250.25,
  'trends': [
    {
      'monthLabel': 'Ene',
      'totalLiters': 380.5,
      'totalCost': 8200.50,
      'averagePricePerLiter': 21.55,
      'averageEfficiency': 8.4,
    },
    {
      'monthLabel': 'Feb',
      'totalLiters': 412.75,
      'totalCost': 9180.25,
      'averagePricePerLiter': 22.24,
      'averageEfficiency': 7.9,
    },
  ],
};

const _expenseByAccountData = {
  'grandTotal': 48250.75,
  'totalApproved': 32,
  'totalPending': 7,
  'accounts': [
    {
      'accountCode': '6.1.02',
      'accountName': 'Combustible y Lubricantes',
      'totalAmount': 22000.0,
      'approvedCount': 18,
      'pendingCount': 3,
    },
    {
      'accountCode': '6.1.04',
      'accountName': 'Mantenimiento y Reparaciones',
      'totalAmount': 14250.75,
      'approvedCount': 9,
      'pendingCount': 2,
    },
  ],
};

const _vehicleUsageData = {
  'vehicles': [
    {
      'plate': 'ABC-123',
      'brandModel': 'Isuzu NPR 2021',
      'status': 'Active',
      'totalKm': 12450.5,
      'tripCount': 34,
      'averageKmPerTrip': 366.2,
    },
    {
      'plate': 'XYZ-987',
      'brandModel': 'Toyota Hilux 2022',
      'status': 'InMaintenance',
      'totalKm': 8230.0,
      'tripCount': 21,
      'averageKmPerTrip': 391.9,
    },
  ],
};

// ── Notifiers de prueba (build() poblado, loads sin red) ──
class _AlertsNotifier extends FleetAlertNotifier {
  @override
  FleetAlertState build() => _alertsState();

  @override
  Future<void> loadSummary() async {}

  @override
  Future<void> loadActiveAlerts() async {}

  @override
  Future<void> dispatchNotifications() async {}
}

class _PredictiveNotifier extends FleetPredictiveNotifier {
  @override
  FleetPredictiveState build() => _predictiveState();

  @override
  Future<void> loadSummary() async {}

  @override
  Future<void> loadForecasts({String? vehicleId}) async {}

  @override
  Future<void> loadFuelAnomalies({
    String? vehicleId,
    DateTime? from,
    DateTime? to,
  }) async {}

  @override
  Future<void> loadFuelTrends({String? vehicleId}) async {}
}

class _TrackingNotifier extends FleetTrackingNotifier {
  _TrackingNotifier(this.initial);

  final FleetTrackingState initial;

  @override
  FleetTrackingState build() => initial;

  @override
  Future<void> loadRecentDeliveries() async {}

  @override
  Future<void> loadTrackingTimeline(String deliveryId) async {}

  @override
  Future<void> sendEtaNotification(String deliveryId) async {}
}

class _ReportsNotifier extends FleetReportNotifier {
  _ReportsNotifier(this.initial);

  final FleetReportState initial;

  @override
  FleetReportState build() => initial;

  @override
  Future<void> loadReport() async {}

  @override
  Future<void> exportReport(String format) async {}

  @override
  void setTab(FleetReportTab tab) {
    state = state.copyWith(
      activeTab: tab,
      activeReport: reportsForTab(tab).first,
    );
  }

  @override
  void setReport(FleetReportType report) {
    state = state.copyWith(activeReport: report);
  }

  @override
  void setDateRange(DateTime? start, DateTime? end) {}
}

// ── Harness ──
Widget _alertsApp() => ProviderScope(
      overrides: [
        fleetAlertProvider.overrideWith(_AlertsNotifier.new),
      ],
      child: const MaterialApp(home: FleetAlertsPage()),
    );

Widget _predictiveApp() => ProviderScope(
      overrides: [
        fleetPredictiveProvider.overrideWith(_PredictiveNotifier.new),
      ],
      child: const MaterialApp(home: FleetPredictivePage()),
    );

Widget _trackingApp(FleetTrackingState state) => ProviderScope(
      overrides: [
        fleetTrackingProvider.overrideWith(() => _TrackingNotifier(state)),
      ],
      child: const MaterialApp(home: FleetTrackingPage()),
    );

Widget _reportsApp(FleetReportState state) => ProviderScope(
      overrides: [
        fleetReportProvider.overrideWith(() => _ReportsNotifier(state)),
      ],
      child: const MaterialApp(home: FleetReportsPage()),
    );

/// Recorre las 3 pestañas de una página con `TabBar` y verifica que ninguna
/// desborde en el ancho actual.
Future<void> _sweepTabs(WidgetTester tester, double width, Widget app) async {
  await pumpAt(tester, width, app);
  expect(tester.takeException(), isNull, reason: 'Desborde a ${width.toInt()} px');

  for (final tab in const ['Combustible', 'Tendencias']) {
    await tester.tap(find.text(tab));
    await tester.pumpAndSettle();
    expect(
      tester.takeException(),
      isNull,
      reason: 'Desborde en la pestaña "$tab" a ${width.toInt()} px',
    );
  }
}

void main() {
  group('FleetAlertsPage', () {
    testWidgets('tarjetas de KPI y alertas caben en teléfono', (tester) async {
      await pumpAcrossPhones(tester, (_) => _alertsApp(), height: 1400);

      expect(find.text('Total Alertas'), findsOneWidget);
      expect(
        find.text('Mantenimiento vencido — Camión ABC-123'),
        findsOneWidget,
      );
    });
  });

  group('FleetPredictivePage', () {
    testWidgets('las 3 pestañas caben en teléfono', (tester) async {
      for (final width in phoneWidths) {
        await _sweepTabs(tester, width, _predictiveApp());
      }

      expect(find.text('Tendencias de Consumo por Vehículo'), findsOneWidget);
    });
  });

  group('FleetTrackingPage', () {
    testWidgets('lista de entregas cabe en teléfono', (tester) async {
      await pumpAcrossPhones(
        tester,
        (_) => _trackingApp(const FleetTrackingState(
          recentDeliveries: _trackingDeliveries,
        )),
        height: 900,
      );

      expect(find.text('Entregas Recientes'), findsOneWidget);
    });

    testWidgets('detalle con timeline cabe en teléfono', (tester) async {
      await pumpAcrossPhones(
        tester,
        (_) => _trackingApp(const FleetTrackingState(
          deliveryDetail: _trackingDetail,
          timeline: _trackingTimeline,
        )),
        height: 1600,
      );

      // Los 2 botones de acción ya bajaron de línea (Wrap) sin desbordar.
      expect(find.text('Enviar ETA'), findsOneWidget);
      expect(find.text('Confirmar Entrega'), findsOneWidget);
    });
  });

  group('FleetReportsPage', () {
    testWidgets('KPIs de flota y barra de exportación caben en teléfono',
        (tester) async {
      await pumpAcrossPhones(
        tester,
        (_) => _reportsApp(const FleetReportState(
          activeReport: FleetReportType.fleetKpi,
          data: _fleetKpiData,
        )),
        height: 2200,
      );

      expect(find.text('KPIs de Flota'), findsOneWidget);
      expect(find.text('Total Vehículos'), findsOneWidget);
      expect(find.text('Excel'), findsOneWidget);
    });

    testWidgets('vistas de contenido de cada reporte caben en teléfono',
        (tester) async {
      const cases = <(FleetReportType, Map<String, dynamic>)>[
        (FleetReportType.costSummary, _costSummaryData),
        (FleetReportType.costTrend, _costTrendData),
        (FleetReportType.fuelTrend, _fuelTrendData),
        (FleetReportType.expenseByAccount, _expenseByAccountData),
        (FleetReportType.vehicleUsage, _vehicleUsageData),
      ];

      for (final (type, data) in cases) {
        await pumpAcrossPhones(
          tester,
          (_) => _reportsApp(
            FleetReportState(activeReport: type, data: data),
          ),
          height: 2200,
        );
      }

      expect(find.text('Uso de Vehículos'), findsOneWidget);
    });

    testWidgets('las 3 pestañas y el estado vacío caben en teléfono',
        (tester) async {
      for (final width in phoneWidths) {
        await pumpAt(tester, width, _reportsApp(const FleetReportState()));
        expect(tester.takeException(), isNull,
            reason: 'Desborde a ${width.toInt()} px');

        for (final tab in const ['Financieros', 'Gerenciales']) {
          await tester.tap(find.text(tab));
          await tester.pumpAndSettle();
          expect(
            tester.takeException(),
            isNull,
            reason: 'Desborde en la pestaña "$tab" a ${width.toInt()} px',
          );
        }
      }

      expect(find.text('Selecciona un reporte'), findsOneWidget);
    });
  });
}
