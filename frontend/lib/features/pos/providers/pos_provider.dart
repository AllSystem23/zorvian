import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

/// POS Cart Item
class PosCartItem {
  final String productId;
  final String name;
  final String code;
  final double price;
  final double quantity;
  final double discount;

  const PosCartItem({
    required this.productId,
    required this.name,
    required this.code,
    required this.price,
    this.quantity = 1,
    this.discount = 0,
  });

  double get subtotal => (price * quantity) - discount;
  double get total => subtotal;

  PosCartItem copyWith({double? quantity, double? discount}) {
    return PosCartItem(
      productId: productId,
      name: name,
      code: code,
      price: price,
      quantity: quantity ?? this.quantity,
      discount: discount ?? this.discount,
    );
  }
}

/// POS State
class PosState {
  final List<PosCartItem> items;
  final String? clientId;
  final String? clientName;
  final String paymentMethod;
  final bool loading;
  final String? error;

  const PosState({
    this.items = const [],
    this.clientId,
    this.clientName,
    this.paymentMethod = 'cash',
    this.loading = false,
    this.error,
  });

  double get subtotal => items.fold(0, (sum, i) => sum + i.subtotal);
  double get discount => items.fold(0, (sum, i) => sum + i.discount);
  double get total => subtotal;
  int get itemCount => items.length;

  PosState copyWith({
    List<PosCartItem>? items,
    String? clientId,
    String? clientName,
    String? paymentMethod,
    bool? loading,
    String? error,
  }) {
    return PosState(
      items: items ?? this.items,
      clientId: clientId ?? this.clientId,
      clientName: clientName ?? this.clientName,
      paymentMethod: paymentMethod ?? this.paymentMethod,
      loading: loading ?? this.loading,
      error: error,
    );
  }
}

class PosNotifier extends Notifier<PosState> {
  @override
  PosState build() => const PosState();

  void addItem(PosCartItem item) {
    final existing = state.items.indexWhere((i) => i.productId == item.productId);
    if (existing >= 0) {
      final updated = List<PosCartItem>.from(state.items);
      updated[existing] = updated[existing].copyWith(
        quantity: updated[existing].quantity + item.quantity,
      );
      state = state.copyWith(items: updated);
    } else {
      state = state.copyWith(items: [...state.items, item]);
    }
  }

  void removeItem(String productId) {
    state = state.copyWith(
      items: state.items.where((i) => i.productId != productId).toList(),
    );
  }

  void updateQuantity(String productId, double quantity) {
    if (quantity <= 0) {
      removeItem(productId);
      return;
    }
    final updated = state.items.map((i) {
      if (i.productId == productId) return i.copyWith(quantity: quantity);
      return i;
    }).toList();
    state = state.copyWith(items: updated);
  }

  void clearCart() {
    state = const PosState();
  }

  void setClient(String? id, String? name) {
    state = state.copyWith(clientId: id, clientName: name);
  }

  void setPaymentMethod(String method) {
    state = state.copyWith(paymentMethod: method);
  }

  Future<bool> submitSale() async {
    if (state.items.isEmpty) return false;
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final payload = {
        'clientId': state.clientId ?? '00000000-0000-0000-0000-000000000000',
        'branchId': '00000000-0000-0000-0000-000000000000',
        'currencyCode': 'USD',
        'saleType': state.paymentMethod == 'credit' ? 'credit' : 'cash',
        'discount': state.discount,
        'details': state.items.map((item) => {
          'productId': item.productId,
          'quantity': item.quantity,
          'unitPrice': item.price,
          'discount': item.discount,
        }).toList(),
      };
      await dio.post('sales/cash', data: payload);
      clearCart();
      return true;
    } catch (e) {
      state = state.copyWith(loading: false, error: 'Error al procesar venta: $e');
      return false;
    }
  }
}

final posProvider = NotifierProvider<PosNotifier, PosState>(() {
  return PosNotifier();
});

/// Products for POS search
final posProductsProvider = FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final response = await dio.get('products');
    if (response.data is List) {
      return (response.data as List).cast<Map<String, dynamic>>();
    }
    return [];
  } catch (e) {
    return [];
  }
});

/// Clients for POS
final posClientsProvider = FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final response = await dio.get('clients');
    if (response.data is List) {
      return (response.data as List).cast<Map<String, dynamic>>();
    }
    return [];
  } catch (e) {
    return [];
  }
});