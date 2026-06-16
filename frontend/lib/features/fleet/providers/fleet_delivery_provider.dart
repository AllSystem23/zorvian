import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetDeliveryItem {
  final String id;
  final String code;
  final String? clientName;
  final String deliveryAddress;
  final String scheduledDate;
  final String? vehiclePlate;
  final String? driverName;
  final String status;
  final String createdAt;

  const FleetDeliveryItem({
    required this.id,
    required this.code,
    this.clientName,
    required this.deliveryAddress,
    required this.scheduledDate,
    this.vehiclePlate,
    this.driverName,
    required this.status,
    required this.createdAt,
  });

  factory FleetDeliveryItem.fromJson(Map<String, dynamic> j) => FleetDeliveryItem(
    id: j['id'] as String,
    code: j['code'] as String? ?? '',
    clientName: j['clientName'] as String?,
    deliveryAddress: j['deliveryAddress'] as String? ?? '',
    scheduledDate: j['scheduledDate'] as String? ?? '',
    vehiclePlate: j['vehiclePlate'] as String?,
    driverName: j['driverName'] as String?,
    status: j['status'] as String? ?? 'Pending',
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class FleetDeliveryState {
  final List<FleetDeliveryItem> items;
  final bool loading;
  final String? error;

  const FleetDeliveryState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  FleetDeliveryState copyWith({
    List<FleetDeliveryItem>? items,
    bool? loading,
    String? error,
  }) => FleetDeliveryState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetDeliveryNotifier extends Notifier<FleetDeliveryState> {
  @override
  FleetDeliveryState build() => const FleetDeliveryState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/deliveries');
      final items = ((r.data['items'] as List?) ?? [])
          .map((e) => FleetDeliveryItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = FleetDeliveryState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar entregas', loading: false);
    }
  }

  Future<bool> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/deliveries/$id');
      await load();
      return true;
    } catch (_) {
      return false;
    }
  }
}

final fleetDeliveryProvider = NotifierProvider<FleetDeliveryNotifier, FleetDeliveryState>(FleetDeliveryNotifier.new);
