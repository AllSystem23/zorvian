import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetRouteItem {
  final String id;
  final String code;
  final String name;
  final String type;
  final String scheduledDate;
  final String status;
  final String? vehiclePlate;
  final String? driverName;
  final String originAddress;
  final String? destinationAddress;
  final String createdAt;

  const FleetRouteItem({
    required this.id,
    required this.code,
    required this.name,
    required this.type,
    required this.scheduledDate,
    required this.status,
    this.vehiclePlate,
    this.driverName,
    required this.originAddress,
    this.destinationAddress,
    required this.createdAt,
  });

  factory FleetRouteItem.fromJson(Map<String, dynamic> j) => FleetRouteItem(
    id: j['id'] as String,
    code: j['code'] as String? ?? '',
    name: j['name'] as String? ?? '',
    type: j['type'] as String? ?? '',
    scheduledDate: j['scheduledDate'] as String? ?? '',
    status: j['status'] as String? ?? 'Planned',
    vehiclePlate: j['vehiclePlate'] as String?,
    driverName: j['driverName'] as String?,
    originAddress: j['originAddress'] as String? ?? '',
    destinationAddress: j['destinationAddress'] as String?,
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class FleetRouteState {
  final List<FleetRouteItem> items;
  final bool loading;
  final String? error;

  const FleetRouteState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  FleetRouteState copyWith({
    List<FleetRouteItem>? items,
    bool? loading,
    String? error,
  }) => FleetRouteState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetRouteNotifier extends Notifier<FleetRouteState> {
  @override
  FleetRouteState build() => const FleetRouteState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/routes');
      final items = ((r.data['items'] as List?) ?? [])
          .map((e) => FleetRouteItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = FleetRouteState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar rutas', loading: false);
    }
  }

  Future<bool> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/routes/$id');
      await load();
      return true;
    } catch (_) {
      return false;
    }
  }
}

final fleetRouteProvider = NotifierProvider<FleetRouteNotifier, FleetRouteState>(FleetRouteNotifier.new);
