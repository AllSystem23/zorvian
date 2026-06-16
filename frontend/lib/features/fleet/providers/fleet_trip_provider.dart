import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetTripItem {
  final String id;
  final String code;
  final String vehiclePlate;
  final String vehicleBrandModel;
  final String driverName;
  final String startDateTime;
  final String? endDateTime;
  final String origin;
  final String destination;
  final String status;
  final double startKm;
  final double? endKm;
  final double? totalKm;
  final String createdAt;

  const FleetTripItem({
    required this.id,
    required this.code,
    required this.vehiclePlate,
    required this.vehicleBrandModel,
    required this.driverName,
    required this.startDateTime,
    this.endDateTime,
    required this.origin,
    required this.destination,
    required this.status,
    required this.startKm,
    this.endKm,
    this.totalKm,
    required this.createdAt,
  });

  factory FleetTripItem.fromJson(Map<String, dynamic> j) => FleetTripItem(
    id: j['id'] as String,
    code: j['code'] as String? ?? '',
    vehiclePlate: j['vehiclePlate'] as String? ?? '',
    vehicleBrandModel: j['vehicleBrandModel'] as String? ?? '',
    driverName: j['driverName'] as String? ?? '',
    startDateTime: j['startDateTime'] as String? ?? '',
    endDateTime: j['endDateTime'] as String?,
    origin: j['origin'] as String? ?? '',
    destination: j['destination'] as String? ?? '',
    status: j['status'] as String? ?? 'Planned',
    startKm: (j['startKm'] as num?)?.toDouble() ?? 0,
    endKm: (j['endKm'] as num?)?.toDouble(),
    totalKm: (j['totalKm'] as num?)?.toDouble(),
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class FleetTripState {
  final List<FleetTripItem> items;
  final bool loading;
  final String? error;

  const FleetTripState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  FleetTripState copyWith({
    List<FleetTripItem>? items,
    bool? loading,
    String? error,
  }) => FleetTripState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetTripNotifier extends Notifier<FleetTripState> {
  @override
  FleetTripState build() => const FleetTripState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/trips');
      final items = ((r.data['items'] as List?) ?? [])
          .map((e) => FleetTripItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = FleetTripState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar viajes', loading: false);
    }
  }

  Future<bool> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/trips/$id');
      await load();
      return true;
    } catch (_) {
      return false;
    }
  }
}

final fleetTripProvider = NotifierProvider<FleetTripNotifier, FleetTripState>(FleetTripNotifier.new);
