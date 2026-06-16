import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetWorkshopItem {
  final String id;
  final String name;
  final String? contactPerson;
  final String phone;
  final String? email;
  final String? address;
  final bool isInternal;
  final bool isActive;

  const FleetWorkshopItem({
    required this.id,
    required this.name,
    this.contactPerson,
    required this.phone,
    this.email,
    this.address,
    required this.isInternal,
    required this.isActive,
  });

  factory FleetWorkshopItem.fromJson(Map<String, dynamic> j) => FleetWorkshopItem(
    id: j['id'] as String,
    name: j['name'] as String? ?? '',
    contactPerson: j['contactPerson'] as String?,
    phone: j['phone'] as String? ?? '',
    email: j['email'] as String?,
    address: j['address'] as String?,
    isInternal: j['isInternal'] as bool? ?? false,
    isActive: j['isActive'] as bool? ?? true,
  );
}

final class FleetWorkshopState {
  final List<FleetWorkshopItem> items;
  final bool loading;
  final String? error;

  const FleetWorkshopState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  FleetWorkshopState copyWith({
    List<FleetWorkshopItem>? items,
    bool? loading,
    String? error,
  }) => FleetWorkshopState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetWorkshopNotifier extends Notifier<FleetWorkshopState> {
  @override
  FleetWorkshopState build() => const FleetWorkshopState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/workshops');
      final items = ((r.data as List?) ?? [])
          .map((e) => FleetWorkshopItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = FleetWorkshopState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar talleres', loading: false);
    }
  }

  Future<bool> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/workshops/$id');
      await load();
      return true;
    } catch (_) {
      return false;
    }
  }
}

final fleetWorkshopProvider = NotifierProvider<FleetWorkshopNotifier, FleetWorkshopState>(FleetWorkshopNotifier.new);
