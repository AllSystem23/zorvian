import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';

final class WarehouseItem {
  final String id;
  final String branchId;
  final String? branchName;
  final String name;
  final String? code;
  final String? address;
  final bool isActive;
  final bool isDefault;

  const WarehouseItem({
    required this.id,
    required this.branchId,
    this.branchName,
    required this.name,
    this.code,
    this.address,
    required this.isActive,
    required this.isDefault,
  });

  factory WarehouseItem.fromJson(Map<String, dynamic> j) => WarehouseItem(
    id: j['id'] as String,
    branchId: j['branchId'] as String? ?? '',
    branchName: j['branchName'] as String?,
    name: j['name'] as String? ?? '',
    code: j['code'] as String?,
    address: j['address'] as String?,
    isActive: j['isActive'] as bool? ?? true,
    isDefault: j['isDefault'] as bool? ?? false,
  );
}

final class WarehouseState {
  final List<WarehouseItem> items;
  final bool loading;
  final String? error;
  const WarehouseState({this.items = const [], this.loading = false, this.error});
  WarehouseState copyWith({List<WarehouseItem>? items, bool? loading, String? error}) =>
      WarehouseState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class WarehouseNotifier extends Notifier<WarehouseState> {
  static String? _lastScope;

  @override
  WarehouseState build() {
    final scope = ref.watch(branchScopeProvider);
    if (_lastScope != null && _lastScope != scope) {
      Future.microtask(() => load());
    }
    _lastScope = scope;
    return const WarehouseState();
  }

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('warehouses');
      final data = r.data as List;
      state = WarehouseState(items: data.map((e) => WarehouseItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar bodegas', loading: false);
    }
  }
}

final warehouseProvider =
    NotifierProvider<WarehouseNotifier, WarehouseState>(WarehouseNotifier.new);

/// Bodegas de una sucursal específica (para selects de formularios, p.ej.
/// el destino de transferencias). `branchId` explícito manda sobre el header.
final warehousesOfBranchProvider =
    FutureProvider.autoDispose.family<List<WarehouseItem>, String>((ref, branchId) async {
  final dio = ref.read(dioClientProvider);
  try {
    final r = await dio.get('warehouses', params: {'branchId': branchId});
    if (r.data is! List) return [];
    return (r.data as List)
        .map((e) => WarehouseItem.fromJson(Map<String, dynamic>.from(e as Map)))
        .toList();
  } catch (_) {
    return [];
  }
});
