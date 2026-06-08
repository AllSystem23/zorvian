import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class CostCenterItem {
  final String id;
  final String name;
  final String code;
  final String? description;
  final bool isActive;
  const CostCenterItem({
    required this.id, required this.name, required this.code,
    this.description, required this.isActive,
  });
  factory CostCenterItem.fromJson(Map<String, dynamic> j) => CostCenterItem(
    id: j['id'] as String,
    name: j['name'] as String? ?? '',
    code: j['code'] as String? ?? '',
    description: j['description'] as String?,
    isActive: j['isActive'] as bool? ?? true,
  );
}

final class CostCenterState {
  final List<CostCenterItem> items;
  final bool loading;
  final String? error;
  const CostCenterState({this.items = const [], this.loading = false, this.error});
  CostCenterState copyWith({List<CostCenterItem>? items, bool? loading, String? error}) =>
    CostCenterState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class CostCenterNotifier extends Notifier<CostCenterState> {
  @override
  CostCenterState build() => const CostCenterState();
  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('cost-centers');
      final data = r.data as List;
      state = CostCenterState(items: data.map((e) => CostCenterItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar centros de costo', loading: false);
    }
  }
}

final costCenterProvider = NotifierProvider<CostCenterNotifier, CostCenterState>(CostCenterNotifier.new);
