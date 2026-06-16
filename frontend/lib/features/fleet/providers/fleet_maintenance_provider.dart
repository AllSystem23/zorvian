import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetWorkOrderItem {
  final String id;
  final String number;
  final String vehiclePlate;
  final String vehicleBrandModel;
  final String? driverName;
  final String reportDateTime;
  final String? failureTypeName;
  final String? problemDescription;
  final String priority;
  final String status;
  final String? workshopName;
  final String? mechanicResponsible;
  final double costEst;
  final double costTotal;
  final String createdAt;

  const FleetWorkOrderItem({
    required this.id,
    required this.number,
    required this.vehiclePlate,
    required this.vehicleBrandModel,
    this.driverName,
    required this.reportDateTime,
    this.failureTypeName,
    this.problemDescription,
    required this.priority,
    required this.status,
    this.workshopName,
    this.mechanicResponsible,
    required this.costEst,
    required this.costTotal,
    required this.createdAt,
  });

  factory FleetWorkOrderItem.fromJson(Map<String, dynamic> j) => FleetWorkOrderItem(
    id: j['id'] as String,
    number: j['number'] as String? ?? '',
    vehiclePlate: j['vehiclePlate'] as String? ?? '',
    vehicleBrandModel: j['vehicleBrandModel'] as String? ?? '',
    driverName: j['driverName'] as String?,
    reportDateTime: j['reportDateTime'] as String? ?? '',
    failureTypeName: j['failureTypeName'] as String?,
    problemDescription: j['problemDescription'] as String?,
    priority: j['priority'] as String? ?? 'Medium',
    status: j['status'] as String? ?? 'Reported',
    workshopName: j['workshopName'] as String?,
    mechanicResponsible: j['mechanicResponsible'] as String?,
    costEst: (j['costEst'] as num?)?.toDouble() ?? 0,
    costTotal: (j['costTotal'] as num?)?.toDouble() ?? 0,
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class FleetMaintenanceState {
  final List<FleetWorkOrderItem> items;
  final bool loading;
  final String? error;

  const FleetMaintenanceState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  FleetMaintenanceState copyWith({
    List<FleetWorkOrderItem>? items,
    bool? loading,
    String? error,
  }) => FleetMaintenanceState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetMaintenanceNotifier extends Notifier<FleetMaintenanceState> {
  @override
  FleetMaintenanceState build() => const FleetMaintenanceState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/work-orders');
      final items = ((r.data['items'] as List?) ?? [])
          .map((e) => FleetWorkOrderItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = FleetMaintenanceState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar órdenes de trabajo', loading: false);
    }
  }

  Future<bool> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/work-orders/$id');
      await load();
      return true;
    } catch (_) {
      return false;
    }
  }
}

final fleetMaintenanceProvider = NotifierProvider<FleetMaintenanceNotifier, FleetMaintenanceState>(FleetMaintenanceNotifier.new);
