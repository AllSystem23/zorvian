import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetVehicleItem {
  final String id;
  final String code;
  final String plate;
  final String brandName;
  final String model;
  final int year;
  final String? color;
  final String? vin;
  final double? currentKm;
  final String status;
  final String? driverName;
  final String? branchName;

  const FleetVehicleItem({
    required this.id,
    required this.code,
    required this.plate,
    required this.brandName,
    required this.model,
    required this.year,
    this.color,
    this.vin,
    this.currentKm,
    required this.status,
    this.driverName,
    this.branchName,
  });

  factory FleetVehicleItem.fromJson(Map<String, dynamic> j) => FleetVehicleItem(
    id: j['id'] as String,
    code: j['code'] as String? ?? '',
    plate: j['plate'] as String? ?? '',
    brandName: j['brandName'] as String? ?? '',
    model: j['model'] as String? ?? '',
    year: (j['year'] as num?)?.toInt() ?? DateTime.now().year,
    color: j['color'] as String?,
    vin: j['vin'] as String?,
    currentKm: (j['currentKm'] as num?)?.toDouble(),
    status: j['status'] as String? ?? 'Active',
    driverName: j['driverName'] as String?,
    branchName: j['branchName'] as String?,
  );
}

final class FleetState {
  final List<FleetVehicleItem> items;
  final bool loading;
  final String? error;

  const FleetState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  FleetState copyWith({
    List<FleetVehicleItem>? items,
    bool? loading,
    String? error,
  }) => FleetState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetNotifier extends Notifier<FleetState> {
  @override
  FleetState build() => const FleetState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/vehicles');
      final data = r.data;
      final items = (data['items'] as List)
          .map((e) => FleetVehicleItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = FleetState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar vehículos', loading: false);
    }
  }
}

final fleetProvider = NotifierProvider<FleetNotifier, FleetState>(FleetNotifier.new);
