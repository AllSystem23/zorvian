import 'package:riverpod_annotation/riverpod_annotation.dart';
import '../../../auth/auth_provider.dart';

part 'brand_provider.g.dart';

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

@riverpod
class BrandNotifier extends _$BrandNotifier {
  @override
  Future<List<BrandItem>> build() async {
    final dio = ref.read(dioClientProvider);
    final r = await dio.get('brands');
    final data = r.data as List;
    return data.map((e) => BrandItem.fromJson(e)).toList();
  }
}
