import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class CategoryItem {
  final String id;
  final String name;
  final String? description;
  final bool isActive;
  const CategoryItem({required this.id, required this.name, this.description, required this.isActive});
  factory CategoryItem.fromJson(Map<String, dynamic> j) => CategoryItem(
    id: j['id'] as String,
    name: j['name'] as String? ?? '',
    description: j['description'] as String?,
    isActive: j['isActive'] as bool? ?? true,
  );
}

final class CategoryState {
  final List<CategoryItem> items;
  final bool loading;
  final String? error;
  const CategoryState({this.items = const [], this.loading = false, this.error});
  CategoryState copyWith({List<CategoryItem>? items, bool? loading, String? error}) =>
    CategoryState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class CategoryNotifier extends Notifier<CategoryState> {
  @override
  CategoryState build() => const CategoryState();
  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('categories');
      final data = r.data as List;
      state = CategoryState(items: data.map((e) => CategoryItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar categorías', loading: false);
    }
  }
}

final categoryProvider = NotifierProvider<CategoryNotifier, CategoryState>(CategoryNotifier.new);
