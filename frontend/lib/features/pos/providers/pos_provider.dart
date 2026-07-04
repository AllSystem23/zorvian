import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';

/// POS Cart Item
class PosCartItem {
  final String productId;
  final String name;
  final String code;
  final double price;
  final double quantity;
  final double discount;
  final double taxRate;
  final double stock;

  const PosCartItem({
    required this.productId,
    required this.name,
    required this.code,
    required this.price,
    this.quantity = 1,
    this.discount = 0,
    this.taxRate = 0,
    this.stock = 999,
  });

  double get subtotal => (price * quantity) - discount;
  double get lineTax => (price * quantity - discount) * taxRate;

  PosCartItem copyWith({double? quantity, double? discount}) {
    return PosCartItem(
      productId: productId,
      name: name,
      code: code,
      price: price,
      quantity: quantity ?? this.quantity,
      discount: discount ?? this.discount,
      taxRate: taxRate,
      stock: stock,
    );
  }
}

/// POS State
class PosState {
  final List<PosCartItem> items;
  final String? clientId;
  final String? clientName;
  final String paymentMethod;
  final String? notes;
  final String? paymentReference;
  final String? cashRegisterId;
  final String? cashRegisterName;
  final double downPayment;
  final int installmentCount;
  final double interestRate;
  final bool loading;
  final String? error;
  final Map<String, dynamic>? lastSaleResponse;

  const PosState({
    this.items = const [],
    this.clientId,
    this.clientName,
    this.paymentMethod = 'cash',
    this.notes,
    this.paymentReference,
    this.cashRegisterId,
    this.cashRegisterName,
    this.downPayment = 0,
    this.installmentCount = 1,
    this.interestRate = 0,
    this.loading = false,
    this.error,
    this.lastSaleResponse,
  });

  double get subtotal => items.fold(0, (sum, i) => sum + (i.price * i.quantity));
  double get discount => items.fold(0, (sum, i) => sum + i.discount);
  double get estimatedTax => items.fold(0, (sum, i) => sum + i.lineTax);
  double get total => subtotal - discount + estimatedTax;
  int get itemCount => items.length;

  PosState copyWith({
    List<PosCartItem>? items,
    String? clientId,
    String? clientName,
    String? paymentMethod,
    String? notes,
    String? paymentReference,
    String? cashRegisterId,
    String? cashRegisterName,
    double? downPayment,
    int? installmentCount,
    double? interestRate,
    bool? loading,
    String? error,
    Map<String, dynamic>? lastSaleResponse,
  }) {
    return PosState(
      items: items ?? this.items,
      clientId: clientId ?? this.clientId,
      clientName: clientName ?? this.clientName,
      paymentMethod: paymentMethod ?? this.paymentMethod,
      notes: notes ?? this.notes,
      paymentReference: paymentReference ?? this.paymentReference,
      cashRegisterId: cashRegisterId ?? this.cashRegisterId,
      cashRegisterName: cashRegisterName ?? this.cashRegisterName,
      downPayment: downPayment ?? this.downPayment,
      installmentCount: installmentCount ?? this.installmentCount,
      interestRate: interestRate ?? this.interestRate,
      loading: loading ?? this.loading,
      error: error,
      lastSaleResponse: lastSaleResponse ?? this.lastSaleResponse,
    );
  }
}

class PosNotifier extends Notifier<PosState> {
  @override
  PosState build() => const PosState();

  /// Returns true if the item was added, false if stock insufficient.
  bool addItem(PosCartItem item) {
    final existing = state.items.indexWhere((i) => i.productId == item.productId);
    final currentQty = existing >= 0 ? state.items[existing].quantity : 0.0;
    if (currentQty + item.quantity > item.stock) {
      state = state.copyWith(error: 'Stock insuficiente para "${item.name}". Disponible: ${item.stock.toInt()}');
      return false;
    }
    if (existing >= 0) {
      final updated = List<PosCartItem>.from(state.items);
      updated[existing] = updated[existing].copyWith(
        quantity: updated[existing].quantity + item.quantity,
      );
      state = state.copyWith(items: updated, error: null);
    } else {
      state = state.copyWith(items: [...state.items, item], error: null);
    }
    return true;
  }

  void removeItem(String productId) {
    state = state.copyWith(
      items: state.items.where((i) => i.productId != productId).toList(),
    );
  }

  /// Returns true if the quantity was updated, false if stock insufficient.
  bool updateQuantity(String productId, double quantity) {
    if (quantity <= 0) {
      removeItem(productId);
      return true;
    }
    final idx = state.items.indexWhere((i) => i.productId == productId);
    if (idx < 0) return false;
    final item = state.items[idx];
    if (quantity > item.stock) {
      state = state.copyWith(error: 'Stock insuficiente para "${item.name}". Disponible: ${item.stock.toInt()}');
      return false;
    }
    final updated = state.items.map((i) {
      if (i.productId == productId) return i.copyWith(quantity: quantity);
      return i;
    }).toList();
    state = state.copyWith(items: updated, error: null);
    return true;
  }

  void clearCart() {
    state = const PosState();
  }

  void clearError() {
    state = state.copyWith(error: null);
  }

  void setClient(String? id, String? name) {
    state = state.copyWith(clientId: id, clientName: name);
  }

  void setPaymentMethod(String method) {
    state = state.copyWith(paymentMethod: method);
  }

  void setNotes(String? notes) {
    state = state.copyWith(notes: notes);
  }

  void setPaymentReference(String? reference) {
    state = state.copyWith(paymentReference: reference);
  }

  void setCashRegister(String? id, String? name) {
    state = state.copyWith(cashRegisterId: id, cashRegisterName: name);
  }

  void setDownPayment(double value) {
    state = state.copyWith(downPayment: value);
  }

  void setInstallmentCount(int value) {
    state = state.copyWith(installmentCount: value);
  }

  void setInterestRate(double value) {
    state = state.copyWith(interestRate: value);
  }

  void setItemDiscount(String productId, double discount) {
    final updated = state.items.map((i) {
      if (i.productId == productId) return i.copyWith(discount: discount);
      return i;
    }).toList();
    state = state.copyWith(items: updated);
  }

  void clearLastSaleResponse() {
    state = state.copyWith(lastSaleResponse: null);
  }

  Future<bool> submitSale() async {
    if (state.items.isEmpty) return false;
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final auth = ref.read(authProvider);
      final branch = ref.read(companyBranchProvider);

      final details = state.items.map((item) => {
        'productId': item.productId,
        'productName': item.name,
        'quantity': item.quantity,
        'unitPrice': item.price,
        'discount': item.discount,
        'subtotal': item.subtotal,
      }).toList();

      final base = {
        'clientId': state.clientId ?? '00000000-0000-0000-0000-000000000000',
        'employeeId': auth.employeeId ?? '00000000-0000-0000-0000-000000000000',
        'discount': state.discount,
        'notes': state.notes,
        'branchId': branch.branchId ?? '00000000-0000-0000-0000-000000000000',
        'currencyCode': auth.currencyCode,
        'exchangeRateToReporting': null,
        'details': details,
      };

      final isCredit = state.paymentMethod == 'credit';

      final response = isCredit
          ? await dio.post('sales/credit', data: {
              ...base,
              'downPayment': state.downPayment,
              'installmentCount': state.installmentCount,
              'interestRate': state.interestRate,
            })
          : await dio.post('sales/cash', data: {
              ...base,
              'payment': {
                'amount': state.total,
                'paymentMethod': state.paymentMethod,
                'referenceNumber': state.paymentReference,
                'cashRegisterId': state.cashRegisterId,
              },
            });

      final saleData = response.data is Map<String, dynamic>
          ? response.data as Map<String, dynamic>
          : <String, dynamic>{};
      state = state.copyWith(lastSaleResponse: saleData, loading: false);
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
    final response = await dio.get('products', params: {
      'PageSize': 9999,
      'IsActive': true,
    });
    if (response.data is Map) {
      final data = response.data as Map<String, dynamic>;
      final items = data['items'];
      if (items is List) {
        return items.map((item) {
          final m = item as Map<String, dynamic>;
          return <String, dynamic>{
            'id': m['id'],
            'name': m['name'],
            'code': m['code'],
            'category': m['categoryName'],
            'price': (m['sellingPrice'] ?? 0).toDouble(),
            'stock': (m['stock'] ?? 0).toDouble(),
            'minStock': (m['minStock'] ?? 0).toDouble(),
            'taxRate': (m['taxRate'] ?? 0).toDouble(),
          };
        }).toList();
      }
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
    final response = await dio.get('clients', params: {
      'PageSize': 9999,
    });
    if (response.data is Map) {
      final data = response.data as Map<String, dynamic>;
      final items = data['items'];
      if (items is List) {
        return items.cast<Map<String, dynamic>>();
      }
    }
    return [];
  } catch (e) {
    return [];
  }
});

/// Open cash registers for the current branch
final posCashRegistersProvider = FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final branch = ref.read(companyBranchProvider);
  try {
    final response = await dio.get('cash-registers', params: {
      'branchId': branch.branchId,
      'status': 'open',
    });
    if (response.data is List) {
      return (response.data as List).cast<Map<String, dynamic>>();
    }
    if (response.data is Map && (response.data as Map)['data'] is List) {
      return ((response.data as Map)['data'] as List).cast<Map<String, dynamic>>();
    }
    return [];
  } catch (e) {
    return [];
  }
});

/// Current accounting period status.
/// Returns null if no open period exists for the current month.
final posCurrentPeriodProvider = FutureProvider.autoDispose<Map<String, dynamic>?>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final response = await dio.get('accounting-periods');
    if (response.data is List) {
      final now = DateTime.now();
      final periods = (response.data as List).cast<Map<String, dynamic>>();
      final current = periods.firstWhere(
        (p) => p['year'] == now.year && p['month'] == now.month,
        orElse: () => <String, dynamic>{},
      );
      if (current.isNotEmpty) return current;
    }
    if (response.data is Map && (response.data as Map)['data'] is List) {
      final now = DateTime.now();
      final periods = ((response.data as Map)['data'] as List).cast<Map<String, dynamic>>();
      final current = periods.firstWhere(
        (p) => p['year'] == now.year && p['month'] == now.month,
        orElse: () => <String, dynamic>{},
      );
      if (current.isNotEmpty) return current;
    }
    return null;
  } catch (e) {
    return null;
  }
});
