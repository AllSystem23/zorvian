import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class BrandItem {
  final String id;
  final String name;
  final String? description;
  final bool isActive;
  const BrandItem({required this.id, required this.name, this.description, required this.isActive});
  factory BrandItem.fromJson(Map<String, dynamic> j) => BrandItem(
    id: j['id'] as String,
    name: j['name'] as String? ?? '',
    description: j['description'] as String?,
    isActive: j['isActive'] as bool? ?? true,
  );
}

final class BrandState {
  final List<BrandItem> items;
  final bool loading;
  final String? error;
  const BrandState({this.items = const [], this.loading = false, this.error});
  BrandState copyWith({List<BrandItem>? items, bool? loading, String? error}) =>
    BrandState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class BrandNotifier extends Notifier<BrandState> {
  @override
  BrandState build() => const BrandState();
  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('brands');
      final data = r.data as List;
      state = BrandState(items: data.map((e) => BrandItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar marcas', loading: false);
    }
  }
}

final brandProvider = NotifierProvider<BrandNotifier, BrandState>(BrandNotifier.new);
