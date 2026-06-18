import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetDashboardData {
  final int totalVehicles;
  final int activeVehicles;
  final int inMaintenance;
  final int availableDrivers;
  final int activeRoutes;
  final int pendingDeliveries;
  final int tripsToday;
  final int expiringDocuments;
  final int overdueMaintenance;
  final List<AlertItem> alerts;

  const FleetDashboardData({
    this.totalVehicles = 0,
    this.activeVehicles = 0,
    this.inMaintenance = 0,
    this.availableDrivers = 0,
    this.activeRoutes = 0,
    this.pendingDeliveries = 0,
    this.tripsToday = 0,
    this.expiringDocuments = 0,
    this.overdueMaintenance = 0,
    this.alerts = const [],
  });

  factory FleetDashboardData.fromJson(Map<String, dynamic> j) =>
      FleetDashboardData(
        totalVehicles: (j['totalVehicles'] as num?)?.toInt() ?? 0,
        activeVehicles: (j['activeVehicles'] as num?)?.toInt() ?? 0,
        inMaintenance: (j['inMaintenance'] as num?)?.toInt() ?? 0,
        availableDrivers: (j['availableDrivers'] as num?)?.toInt() ?? 0,
        activeRoutes: (j['activeRoutes'] as num?)?.toInt() ?? 0,
        pendingDeliveries: (j['pendingDeliveries'] as num?)?.toInt() ?? 0,
        tripsToday: (j['tripsToday'] as num?)?.toInt() ?? 0,
        expiringDocuments: (j['expiringDocuments'] as num?)?.toInt() ?? 0,
        overdueMaintenance: (j['overdueMaintenance'] as num?)?.toInt() ?? 0,
        alerts: ((j['alerts'] as List?) ?? [])
            .whereType<Map<String, dynamic>>()
            .map((e) => AlertItem.fromJson(e))
            .toList(),
      );
}

final class AlertItem {
  final String type;
  final String severity;
  final String message;
  final String? entityId;
  final String? entityType;

  const AlertItem({
    required this.type,
    required this.severity,
    required this.message,
    this.entityId,
    this.entityType,
  });

  factory AlertItem.fromJson(Map<String, dynamic> j) => AlertItem(
    type: j['type'] as String? ?? '',
    severity: j['severity'] as String? ?? '',
    message: j['message'] as String? ?? '',
    entityId: j['entityId'] as String?,
    entityType: j['entityType'] as String?,
  );
}

final class FleetDashboardState {
  final FleetDashboardData data;
  final bool loading;
  final String? error;

  const FleetDashboardState({
    this.data = const FleetDashboardData(),
    this.loading = false,
    this.error,
  });

  FleetDashboardState copyWith({
    FleetDashboardData? data,
    bool? loading,
    String? error,
  }) => FleetDashboardState(
    data: data ?? this.data,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetDashboardNotifier extends Notifier<FleetDashboardState> {
  @override
  FleetDashboardState build() => const FleetDashboardState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/dashboard');
      final data = r.data;
      state = FleetDashboardState(
        data: data is Map<String, dynamic>
            ? FleetDashboardData.fromJson(data)
            : const FleetDashboardData(),
      );
    } on DioException {
      state = state.copyWith(
        error: 'No se pudo cargar el dashboard de flota',
        loading: false,
      );
    } catch (_) {
      state = state.copyWith(
        error: 'Error al cargar dashboard',
        loading: false,
      );
    }
  }
}

final fleetDashboardProvider =
    NotifierProvider<FleetDashboardNotifier, FleetDashboardState>(
      FleetDashboardNotifier.new,
    );
