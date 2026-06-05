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

final class InventoryMovementState {
  final List<InventoryMovementItem> items;
  final bool loading;
  final String? error;
  const InventoryMovementState({this.items = const [], this.loading = false, this.error});
  InventoryMovementState copyWith({List<InventoryMovementItem>? items, bool? loading, String? error}) =>
    InventoryMovementState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class InventoryMovementNotifier extends Notifier<InventoryMovementState> {
  @override
  InventoryMovementState build() => const InventoryMovementState();
  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('inventory-movements');
      final data = r.data;
      state = InventoryMovementState(items: (data['items'] as List).map((e) => InventoryMovementItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar movimientos', loading: false);
    }
  }
}

final inventoryMovementProvider = NotifierProvider<InventoryMovementNotifier, InventoryMovementState>(InventoryMovementNotifier.new);
