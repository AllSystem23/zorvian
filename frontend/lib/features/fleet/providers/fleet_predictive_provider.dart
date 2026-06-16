import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

// ── Predictive Maintenance State ──

class FleetPredictiveState {
  final Map<String, dynamic>? maintenanceSummary;
  final Map<String, dynamic>? fuelSummary;
  final List<Map<String, dynamic>> forecasts;
  final List<Map<String, dynamic>> fuelAnomalies;
  final List<Map<String, dynamic>> fuelTrends;
  final bool loading;
  final String? error;

  const FleetPredictiveState({
    this.maintenanceSummary,
    this.fuelSummary,
    this.forecasts = const [],
    this.fuelAnomalies = const [],
    this.fuelTrends = const [],
    this.loading = false,
    this.error,
  });

  FleetPredictiveState copyWith({
    Map<String, dynamic>? maintenanceSummary,
    Map<String, dynamic>? fuelSummary,
    List<Map<String, dynamic>>? forecasts,
    List<Map<String, dynamic>>? fuelAnomalies,
    List<Map<String, dynamic>>? fuelTrends,
    bool? loading,
    String? error,
    bool clearError = false,
  }) =>
      FleetPredictiveState(
        maintenanceSummary: maintenanceSummary ?? this.maintenanceSummary,
        fuelSummary: fuelSummary ?? this.fuelSummary,
        forecasts: forecasts ?? this.forecasts,
        fuelAnomalies: fuelAnomalies ?? this.fuelAnomalies,
        fuelTrends: fuelTrends ?? this.fuelTrends,
        loading: loading ?? this.loading,
        error: clearError ? null : (error ?? this.error),
      );
}

// ── Predictive Maintenance Notifier ──

class FleetPredictiveNotifier extends Notifier<FleetPredictiveState> {
  @override
  FleetPredictiveState build() => const FleetPredictiveState();

  Future<void> loadSummary() async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/predictive/maintenance/summary');
      state = state.copyWith(
        maintenanceSummary: Map<String, dynamic>.from(r.data as Map),
        loading: false,
      );
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar resumen predictivo', loading: false);
    }
  }

  Future<void> loadForecasts({String? vehicleId}) async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final qs = vehicleId != null ? '?vehicleId=$vehicleId' : '';
      final r = await dio.get('fleet/predictive/maintenance/forecasts$qs');
      final items = (r.data as List).map((e) => Map<String, dynamic>.from(e as Map)).toList();
      state = state.copyWith(forecasts: items, loading: false);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar pronósticos', loading: false);
    }
  }

  Future<void> loadFuelAnomalies({String? vehicleId, DateTime? from, DateTime? to}) async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, String>{};
      if (vehicleId != null) params['vehicleId'] = vehicleId;
      if (from != null) params['fromDate'] = from.toIso8601String();
      if (to != null) params['toDate'] = to.toIso8601String();
      final qs = params.isNotEmpty ? '?${Uri(queryParameters: params).query}' : '';
      final r = await dio.get('fleet/predictive/fuel/anomalies$qs');
      state = state.copyWith(
        fuelAnomalies: (r.data as Map<String, dynamic>)['recentAnomalies'] != null
            ? ((r.data['recentAnomalies'] as List).map((e) => Map<String, dynamic>.from(e as Map)).toList())
            : [],
        fuelSummary: Map<String, dynamic>.from(r.data as Map),
        loading: false,
      );
    } catch (_) {
      state = state.copyWith(error: 'Error al detectar anomalías de combustible', loading: false);
    }
  }

  Future<void> loadFuelTrends({String? vehicleId}) async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final qs = vehicleId != null ? '?vehicleId=$vehicleId' : '';
      final r = await dio.get('fleet/predictive/fuel/trends$qs');
      final items = (r.data as List).map((e) => Map<String, dynamic>.from(e as Map)).toList();
      state = state.copyWith(fuelTrends: items, loading: false);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar tendencias', loading: false);
    }
  }

  Future<void> markAnomaly(String refillId, bool isAnomaly, {String? notes}) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('fleet/predictive/fuel/$refillId/anomaly', data: {
        'isAnomaly': isAnomaly,
        'notes': notes,
      });
    } catch (_) {
      state = state.copyWith(error: 'Error al marcar anomalía');
    }
  }
}

final fleetPredictiveProvider =
    NotifierProvider<FleetPredictiveNotifier, FleetPredictiveState>(FleetPredictiveNotifier.new);
