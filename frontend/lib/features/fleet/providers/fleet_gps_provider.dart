import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

// ── GPS State ──

class FleetGpsState {
  final List<Map<String, dynamic>> vehiclePositions;
  final Map<String, dynamic>? selectedVehicle;
  final List<Map<String, dynamic>> routeHistory;
  final List<Map<String, dynamic>> geofences;
  final bool loading;
  final String? error;

  const FleetGpsState({
    this.vehiclePositions = const [],
    this.selectedVehicle,
    this.routeHistory = const [],
    this.geofences = const [],
    this.loading = false,
    this.error,
  });

  FleetGpsState copyWith({
    List<Map<String, dynamic>>? vehiclePositions,
    Map<String, dynamic>? selectedVehicle,
    List<Map<String, dynamic>>? routeHistory,
    List<Map<String, dynamic>>? geofences,
    bool? loading,
    String? error,
    bool clearSelected = false,
    bool clearError = false,
  }) =>
      FleetGpsState(
        vehiclePositions: vehiclePositions ?? this.vehiclePositions,
        selectedVehicle: clearSelected ? null : (selectedVehicle ?? this.selectedVehicle),
        routeHistory: routeHistory ?? this.routeHistory,
        geofences: geofences ?? this.geofences,
        loading: loading ?? this.loading,
        error: clearError ? null : (error ?? this.error),
      );
}

// ── GPS Notifier ──

class FleetGpsNotifier extends Notifier<FleetGpsState> {
  @override
  FleetGpsState build() => const FleetGpsState();

  void clearSelected() {
    state = state.copyWith(clearSelected: true);
  }

  Future<void> loadFleetPositions() async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/gps/fleet');
      final data = (r.data as List).map((e) => Map<String, dynamic>.from(e as Map)).toList();
      state = state.copyWith(vehiclePositions: data, loading: false);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar posiciones GPS', loading: false);
    }
  }

  Future<void> selectVehicle(String vehicleId) async {
    state = state.copyWith(loading: true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/gps/vehicles/$vehicleId/latest');
      state = state.copyWith(selectedVehicle: Map<String, dynamic>.from(r.data as Map), loading: false);
    } catch (_) {
      state = state.copyWith(loading: false);
    }
  }

  Future<void> loadRouteHistory(String vehicleId, DateTime from, DateTime to) async {
    try {
      final dio = ref.read(dioClientProvider);
      final params = {
        'from': from.toIso8601String(),
        'to': to.toIso8601String(),
      };
      final qs = '?${Uri(queryParameters: params).query}';
      final r = await dio.get('fleet/gps/vehicles/$vehicleId/history$qs');
      final data = Map<String, dynamic>.from(r.data as Map);
      final positions = (data['positions'] as List?)?.map((e) => Map<String, dynamic>.from(e as Map)).toList() ?? [];
      state = state.copyWith(routeHistory: positions);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar historial');
    }
  }

  Future<void> loadGeofences() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/geofences');
      final items = (r.data as Map<String, dynamic>)['items'] as List? ?? [];
      state = state.copyWith(geofences: items.map((e) => Map<String, dynamic>.from(e as Map)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar geocercas');
    }
  }
}

final fleetGpsProvider =
    NotifierProvider<FleetGpsNotifier, FleetGpsState>(FleetGpsNotifier.new);
