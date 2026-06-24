import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

// ── Tracking State ──

class FleetTrackingState {
  final Map<String, dynamic>? deliveryDetail;
  final List<Map<String, dynamic>> timeline;
  final List<Map<String, dynamic>> recentDeliveries;
  final bool loading;
  final String? error;
  final bool confirming;

  const FleetTrackingState({
    this.deliveryDetail,
    this.timeline = const [],
    this.recentDeliveries = const [],
    this.loading = false,
    this.error,
    this.confirming = false,
  });

  FleetTrackingState copyWith({
    Map<String, dynamic>? deliveryDetail,
    List<Map<String, dynamic>>? timeline,
    List<Map<String, dynamic>>? recentDeliveries,
    bool? loading,
    String? error,
    bool? confirming,
    bool clearDetail = false,
    bool clearError = false,
  }) =>
      FleetTrackingState(
        deliveryDetail: clearDetail ? null : (deliveryDetail ?? this.deliveryDetail),
        timeline: timeline ?? this.timeline,
        recentDeliveries: recentDeliveries ?? this.recentDeliveries,
        loading: loading ?? this.loading,
        error: clearError ? null : (error ?? this.error),
        confirming: confirming ?? this.confirming,
      );
}

// ── Tracking Notifier ──

class FleetTrackingNotifier extends Notifier<FleetTrackingState> {
  @override
  FleetTrackingState build() => const FleetTrackingState();

  void clearDetail() {
    state = state.copyWith(clearDetail: true);
  }

  Future<void> loadRecentDeliveries() async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/deliveries');
      final items = (r.data as Map<String, dynamic>)['items'] as List? ?? [];
      state = state.copyWith(
        recentDeliveries: items.map((e) => Map<String, dynamic>.from(e as Map)).toList(),
        loading: false,
      );
    } catch (e) {
      final msg = e is DioException
          ? (e.response?.data is Map
              ? (e.response?.data['detail'] ?? e.response?.data['message'] ?? 'Error de conexión')
              : e.message ?? 'Error de conexión')
          : e.toString();
      state = state.copyWith(error: msg, loading: false);
    }
  }

  Future<void> loadTrackingTimeline(String deliveryId) async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/deliveries/$deliveryId/timeline');
      final data = Map<String, dynamic>.from(r.data as Map);
      final events = (data['events'] as List?)?.map((e) => Map<String, dynamic>.from(e as Map)).toList() ?? [];
      state = state.copyWith(
        deliveryDetail: data,
        timeline: events,
        loading: false,
      );
    } catch (e) {
      final msg = e is DioException
          ? (e.response?.data is Map
              ? (e.response?.data['detail'] ?? e.response?.data['message'] ?? 'Error de conexión')
              : e.message ?? 'Error de conexión')
          : e.toString();
      state = state.copyWith(error: msg, loading: false);
    }
  }

  Future<void> confirmDelivery(String deliveryId, String receiverName, String receiverId) async {
    state = state.copyWith(confirming: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('fleet/deliveries/$deliveryId/confirm', data: {
        'receiverName': receiverName,
        'receiverId': receiverId,
      });
      state = state.copyWith(confirming: false);
      await loadTrackingTimeline(deliveryId);
    } catch (e) {
      final msg = e is DioException
          ? (e.response?.data is Map
              ? (e.response?.data['detail'] ?? e.response?.data['message'] ?? 'Error al confirmar entrega')
              : e.message ?? 'Error al confirmar entrega')
          : e.toString();
      state = state.copyWith(confirming: false, error: msg);
    }
  }

  Future<void> updateStatus(String deliveryId, String status) async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('fleet/deliveries/$deliveryId/status', data: {'status': status});
      state = state.copyWith(loading: false);
      await loadTrackingTimeline(deliveryId);
    } catch (e) {
      final msg = e is DioException
          ? (e.response?.data is Map
              ? (e.response?.data['detail'] ?? e.response?.data['message'] ?? 'Error de conexión')
              : e.message ?? 'Error de conexión')
          : e.toString();
      state = state.copyWith(error: msg, loading: false);
    }
  }

  Future<void> sendEtaNotification(String deliveryId) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('fleet/deliveries/$deliveryId/notify-eta', data: {'notificationType': 'push'});
    } catch (e) {
      final msg = e is DioException
          ? (e.response?.data is Map
              ? (e.response?.data['detail'] ?? e.response?.data['message'] ?? 'Error al enviar notificación ETA')
              : e.message ?? 'Error al enviar notificación ETA')
          : e.toString();
      state = state.copyWith(error: msg);
    }
  }

  Future<void> loadClientTracking(String code) async {
    state = state.copyWith(loading: true, clearError: true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/deliveries/track/$code');
      final data = Map<String, dynamic>.from(r.data as Map);
      final events = (data['events'] as List?)?.map((e) => Map<String, dynamic>.from(e as Map)).toList() ?? [];
      state = state.copyWith(
        deliveryDetail: data,
        timeline: events,
        loading: false,
      );
    } catch (e) {
      final msg = e is DioException
          ? (e.response?.data is Map
              ? (e.response?.data['detail'] ?? e.response?.data['message'] ?? 'Error de conexión')
              : e.message ?? 'Error de conexión')
          : e.toString();
      state = state.copyWith(error: msg, loading: false);
    }
  }
}

final fleetTrackingProvider =
    NotifierProvider<FleetTrackingNotifier, FleetTrackingState>(FleetTrackingNotifier.new);
