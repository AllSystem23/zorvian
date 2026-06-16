import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

// ── Report type enums ──

enum FleetReportTab { operational, financial, managerial }

enum FleetReportType {
  vehicleUsage,
  delivery,
  route,
  costSummary,
  costByVehicle,
  costTrend,
  profitability,
  fleetKpi,
  driverScorecard,
  vehicleScorecard,
  fuelTrend,
  expenseByAccount,
}

extension FleetReportTypeExt on FleetReportType {
  String get apiPath => switch (this) {
        FleetReportType.vehicleUsage => 'vehicle-usage',
        FleetReportType.delivery => 'deliveries',
        FleetReportType.route => 'routes',
        FleetReportType.costSummary => 'cost-summary',
        FleetReportType.costByVehicle => 'cost-by-vehicle',
        FleetReportType.costTrend => 'cost-trend',
        FleetReportType.profitability => 'profitability',
        FleetReportType.fleetKpi => 'kpis',
        FleetReportType.driverScorecard => 'driver-scorecard',
        FleetReportType.vehicleScorecard => 'vehicle-scorecard',
        FleetReportType.fuelTrend => 'fuel-trend',
        FleetReportType.expenseByAccount => 'expense-by-account',
      };

  String get label => switch (this) {
        FleetReportType.vehicleUsage => 'Uso de Vehículos',
        FleetReportType.delivery => 'Entregas',
        FleetReportType.route => 'Rutas',
        FleetReportType.costSummary => 'Resumen Costos',
        FleetReportType.costByVehicle => 'Costo por Vehículo',
        FleetReportType.costTrend => 'Tendencia Costos',
        FleetReportType.profitability => 'Rentabilidad',
        FleetReportType.fleetKpi => 'KPIs Flota',
        FleetReportType.driverScorecard => 'Scorecard Conductores',
        FleetReportType.vehicleScorecard => 'Scorecard Vehículos',
        FleetReportType.fuelTrend => 'Tendencia Combustible',
        FleetReportType.expenseByAccount => 'Gastos por Cuenta Contable',
      };

  FleetReportTab get tab => switch (this) {
        FleetReportType.vehicleUsage => FleetReportTab.operational,
        FleetReportType.delivery => FleetReportTab.operational,
        FleetReportType.route => FleetReportTab.operational,
        FleetReportType.costSummary => FleetReportTab.financial,
        FleetReportType.costByVehicle => FleetReportTab.financial,
        FleetReportType.costTrend => FleetReportTab.financial,
        FleetReportType.profitability => FleetReportTab.financial,
        FleetReportType.fleetKpi => FleetReportTab.managerial,
        FleetReportType.driverScorecard => FleetReportTab.managerial,
        FleetReportType.vehicleScorecard => FleetReportTab.managerial,
        FleetReportType.fuelTrend => FleetReportTab.managerial,
        FleetReportType.expenseByAccount => FleetReportTab.financial,
      };
}

List<FleetReportType> reportsForTab(FleetReportTab tab) =>
    FleetReportType.values.where((r) => r.tab == tab).toList();

// ── State ──

class FleetReportState {
  final FleetReportTab activeTab;
  final FleetReportType activeReport;
  final DateTime? startDate;
  final DateTime? endDate;
  final Map<String, dynamic>? data;
  final bool loading;
  final String? error;
  final bool exporting;

  const FleetReportState({
    this.activeTab = FleetReportTab.operational,
    this.activeReport = FleetReportType.vehicleUsage,
    this.startDate,
    this.endDate,
    this.data,
    this.loading = false,
    this.error,
    this.exporting = false,
  });

  FleetReportState copyWith({
    FleetReportTab? activeTab,
    FleetReportType? activeReport,
    DateTime? startDate,
    DateTime? endDate,
    Map<String, dynamic>? data,
    bool? loading,
    String? error,
    bool? exporting,
    bool clearData = false,
    bool clearError = false,
  }) =>
      FleetReportState(
        activeTab: activeTab ?? this.activeTab,
        activeReport: activeReport ?? this.activeReport,
        startDate: startDate ?? this.startDate,
        endDate: endDate ?? this.endDate,
        data: clearData ? null : (data ?? this.data),
        loading: loading ?? this.loading,
        error: clearError ? null : (error ?? this.error),
        exporting: exporting ?? this.exporting,
      );
}

// ── Notifier ──

class FleetReportNotifier extends Notifier<FleetReportState> {
  @override
  FleetReportState build() => const FleetReportState();

  void setTab(FleetReportTab tab) {
    final firstReport = reportsForTab(tab).first;
    state = state.copyWith(
      activeTab: tab,
      activeReport: firstReport,
      clearData: true,
      clearError: true,
    );
    loadReport();
  }

  void setReport(FleetReportType report) {
    state = state.copyWith(
      activeReport: report,
      clearData: true,
      clearError: true,
    );
    loadReport();
  }

  void setDateRange(DateTime? start, DateTime? end) {
    state = state.copyWith(startDate: start, endDate: end, clearData: true, clearError: true);
    loadReport();
  }

  Future<void> loadReport() async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, String>{};
      if (state.startDate != null) params['startDate'] = state.startDate!.toIso8601String();
      if (state.endDate != null) params['endDate'] = state.endDate!.toIso8601String();

      final qs = params.isNotEmpty ? '?${Uri(queryParameters: params).query}' : '';
      final r = await dio.get('fleet/reports/${state.activeReport.apiPath}$qs');
      state = state.copyWith(data: Map<String, dynamic>.from(r.data), loading: false);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar reporte', loading: false);
    }
  }

  Future<void> exportReport(String format) async {
    state = state.copyWith(exporting: true);
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'reportType': state.activeReport.apiPath,
        'format': format,
      };
      if (state.startDate != null) body['startDate'] = state.startDate!.toIso8601String();
      if (state.endDate != null) body['endDate'] = state.endDate!.toIso8601String();

      await dio.post('fleet/reports/export', data: body);
      state = state.copyWith(exporting: false);
    } catch (_) {
      state = state.copyWith(exporting: false, error: 'Error al exportar');
    }
  }
}

final fleetReportProvider =
    NotifierProvider<FleetReportNotifier, FleetReportState>(FleetReportNotifier.new);
