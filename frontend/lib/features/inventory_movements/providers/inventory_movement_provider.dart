import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class InventoryMovementItem {
  final String id;
  final String productName;
  final String productCode;
  final String type;
  final double quantity;
  final double stockBefore;
  final double stockAfter;
  final String? reference;
  final String? notes;
  final String createdBy;
  final String createdAt;

  const InventoryMovementItem({
    required this.id, required this.productName, required this.productCode,
    required this.type, required this.quantity, required this.stockBefore,
    required this.stockAfter, this.reference, this.notes,
    required this.createdBy, required this.createdAt,
  });

  factory InventoryMovementItem.fromJson(Map<String, dynamic> j) => InventoryMovementItem(
    id: j['id'] as String,
    productName: j['productName'] as String? ?? '',
    productCode: j['productCode'] as String? ?? '',
    type: j['type'] as String? ?? '',
    quantity: (j['quantity'] as num?)?.toDouble() ?? 0,
    stockBefore: (j['stockBefore'] as num?)?.toDouble() ?? 0,
    stockAfter: (j['stockAfter'] as num?)?.toDouble() ?? 0,
    reference: j['reference'] as String?,
    notes: j['notes'] as String?,
    createdBy: j['createdBy'] as String? ?? '',
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class InventoryMovementNotifier extends AsyncNotifier<List<InventoryMovementItem>> {
  @override
  Future<List<InventoryMovementItem>> build() async {
    return _fetch();
  }

  Future<void> load({String? search}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => _fetch(search: search));
  }

  Future<List<InventoryMovementItem>> _fetch({String? search}) async {
    final dio = ref.read(dioClientProvider);
    final params = <String, dynamic>{};
    if (search != null && search.isNotEmpty) params['search'] = search;
    final r = await dio.get('inventory-movements', params: params);
    final data = r.data;
    return (data['items'] as List).map((e) => InventoryMovementItem.fromJson(e)).toList();
  }
}

final inventoryMovementProvider = AsyncNotifierProvider<InventoryMovementNotifier, List<InventoryMovementItem>>(InventoryMovementNotifier.new);
