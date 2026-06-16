import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetFuelItem {
  final String id;
  final String refillDateTime;
  final String vehiclePlate;
  final String vehicleBrandModel;
  final String driverName;
  final String fuelTypeName;
  final double liters;
  final double pricePerLiter;
  final double totalCost;
  final double currentKm;
  final String refillType;
  final String paymentMethod;
  final bool anomalyFlag;
  final String? anomalyNotes;
  final String? supplierName;
  final String createdAt;

  const FleetFuelItem({
    required this.id,
    required this.refillDateTime,
    required this.vehiclePlate,
    required this.vehicleBrandModel,
    required this.driverName,
    required this.fuelTypeName,
    required this.liters,
    required this.pricePerLiter,
    required this.totalCost,
    required this.currentKm,
    required this.refillType,
    required this.paymentMethod,
    required this.anomalyFlag,
    this.anomalyNotes,
    this.supplierName,
    required this.createdAt,
  });

  factory FleetFuelItem.fromJson(Map<String, dynamic> j) => FleetFuelItem(
    id: j['id'] as String,
    refillDateTime: j['refillDateTime'] as String? ?? '',
    vehiclePlate: j['vehiclePlate'] as String? ?? '',
    vehicleBrandModel: j['vehicleBrandModel'] as String? ?? '',
    driverName: j['driverName'] as String? ?? '',
    fuelTypeName: j['fuelTypeName'] as String? ?? '',
    liters: (j['liters'] as num?)?.toDouble() ?? 0,
    pricePerLiter: (j['pricePerLiter'] as num?)?.toDouble() ?? 0,
    totalCost: (j['totalCost'] as num?)?.toDouble() ?? 0,
    currentKm: (j['currentKm'] as num?)?.toDouble() ?? 0,
    refillType: j['refillType'] as String? ?? 'Full',
    paymentMethod: j['paymentMethod'] as String? ?? 'Cash',
    anomalyFlag: j['anomalyFlag'] as bool? ?? false,
    anomalyNotes: j['anomalyNotes'] as String?,
    supplierName: j['supplierName'] as String?,
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class FleetFuelState {
  final List<FleetFuelItem> items;
  final bool loading;
  final String? error;

  const FleetFuelState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  FleetFuelState copyWith({
    List<FleetFuelItem>? items,
    bool? loading,
    String? error,
  }) => FleetFuelState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetFuelNotifier extends Notifier<FleetFuelState> {
  @override
  FleetFuelState build() => const FleetFuelState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/fuel-refills');
      final items = ((r.data['items'] as List?) ?? [])
          .map((e) => FleetFuelItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = FleetFuelState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar combustible', loading: false);
    }
  }

  Future<bool> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/fuel-refills/$id');
      await load();
      return true;
    } catch (_) {
      return false;
    }
  }
}

final fleetFuelProvider = NotifierProvider<FleetFuelNotifier, FleetFuelState>(FleetFuelNotifier.new);
