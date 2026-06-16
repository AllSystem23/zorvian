import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

// ── Alerts State ──

class FleetAlertState {
  final Map<String, dynamic>? summary;
  final List<Map<String, dynamic>> activeAlerts;
  final List<Map<String, dynamic>> drivers;
  final bool loading;
  final String? error;
  final bool dispatching;

  const FleetAlertState({
    this.summary,
    this.activeAlerts = const [],
    this.drivers = const [],
    this.loading = false,
    this.error,
    this.dispatching = false,
  });

  FleetAlertState copyWith({
    Map<String, dynamic>? summary,
    List<Map<String, dynamic>>? activeAlerts,
    List<Map<String, dynamic>>? drivers,
    bool? loading,
    String? error,
    bool? dispatching,
    bool clearError = false,
  }) =>
      FleetAlertState(
        summary: summary ?? this.summary,
        activeAlerts: activeAlerts ?? this.activeAlerts,
        drivers: drivers ?? this.drivers,
        loading: loading ?? this.loading,
        error: clearError ? null : (error ?? this.error),
        dispatching: dispatching ?? this.dispatching,
      );
}

// ── Alerts Notifier ──

class FleetAlertNotifier extends Notifier<FleetAlertState> {
  @override
  FleetAlertState build() => const FleetAlertState();

  Future<void> loadSummary() async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/alerts');
      state = state.copyWith(summary: Map<String, dynamic>.from(r.data as Map), loading: false);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar resumen de alertas', loading: false);
    }
  }

  Future<void> loadActiveAlerts() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/alerts/active');
      final data = (r.data as List).map((e) => Map<String, dynamic>.from(e as Map)).toList();
      state = state.copyWith(activeAlerts: data);
    } catch (_) {}
  }

  Future<void> dispatchNotifications() async {
    state = state.copyWith(dispatching: true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('fleet/alerts/dispatch');
      state = state.copyWith(dispatching: false);
      await loadSummary();
    } catch (_) {
      state = state.copyWith(dispatching: false, error: 'Error al despachar notificaciones');
    }
  }

  Future<void> blockDriver(String driverId, String reason) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('fleet/alerts/drivers/$driverId/block', data: {'reason': reason});
      await loadSummary();
    } catch (_) {
      state = state.copyWith(error: 'Error al bloquear conductor');
    }
  }

  Future<void> unblockDriver(String driverId) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('fleet/alerts/drivers/$driverId/unblock');
      await loadSummary();
    } catch (_) {
      state = state.copyWith(error: 'Error al desbloquear conductor');
    }
  }
}

final fleetAlertProvider =
    NotifierProvider<FleetAlertNotifier, FleetAlertState>(FleetAlertNotifier.new);
