import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class ProductItem {
  final String id;
  final String code;
  final String name;
  final String? description;
  final String? categoryName;
  final String? brandName;
  final double price;
  final double? cost;
  final double stock;
  final double minStock;
  final double maxStock;
  final String unit;
  final bool isActive;

  const ProductItem({
    required this.id, required this.code, required this.name,
    this.description, this.categoryName, this.brandName,
    required this.price, this.cost, required this.stock,
    required this.minStock, required this.maxStock, required this.unit, required this.isActive,
  });

  factory ProductItem.fromJson(Map<String, dynamic> j) => ProductItem(
    id: j['id'] as String,
    code: j['code'] as String? ?? '',
    name: j['name'] as String? ?? '',
    description: j['description'] as String?,
    categoryName: j['categoryName'] as String?,
    brandName: j['brandName'] as String?,
    price: (j['sellingPrice'] as num?)?.toDouble() ?? (j['price'] as num?)?.toDouble() ?? 0,
    cost: (j['costPrice'] as num?)?.toDouble() ?? (j['cost'] as num?)?.toDouble(),
    stock: (j['stock'] as num?)?.toDouble() ?? 0,
    minStock: (j['minStock'] as num?)?.toDouble() ?? 0,
    maxStock: (j['maxStock'] as num?)?.toDouble() ?? 0,
    unit: j['unitOfMeasure'] as String? ?? j['unit'] as String? ?? 'pz',
    isActive: j['isActive'] as bool? ?? true,
  );
}

final class ProductState {
  final List<ProductItem> items;
  final bool loading;
  final String? error;
  const ProductState({this.items = const [], this.loading = false, this.error});
  ProductState copyWith({List<ProductItem>? items, bool? loading, String? error}) =>
    ProductState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class ProductNotifier extends Notifier<ProductState> {
  @override
  ProductState build() => const ProductState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('products');
      final data = r.data;
      state = ProductState(items: (data['items'] as List).map((e) => ProductItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar productos', loading: false);
    }
  }
}

final productProvider = NotifierProvider<ProductNotifier, ProductState>(ProductNotifier.new);
