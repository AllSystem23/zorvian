import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class PurchaseItem {
  final String id;
  final String purchaseNumber;
  final String supplierName;
  final String createdAt;
  final String status;
  final double total;

  const PurchaseItem({
    required this.id, required this.purchaseNumber, required this.supplierName,
    required this.createdAt, required this.status, required this.total,
  });

  factory PurchaseItem.fromJson(Map<String, dynamic> j) => PurchaseItem(
    id: j['id'] as String,
    purchaseNumber: j['purchaseNumber'] as String? ?? '',
    supplierName: j['supplierName'] as String? ?? '',
    createdAt: j['createdAt'] as String? ?? '',
    status: j['status'] as String? ?? '',
    total: (j['total'] as num?)?.toDouble() ?? 0,
  );
}

final class PurchaseDetailItem {
  final String productId;
  final String productName;
  final int quantity;
  final double unitCost;
  final double discount;
  final double subtotal;

  const PurchaseDetailItem({
    required this.productId, required this.productName, required this.quantity,
    required this.unitCost, required this.discount, required this.subtotal,
  });

  factory PurchaseDetailItem.fromJson(Map<String, dynamic> j) => PurchaseDetailItem(
    productId: j['productId'] as String,
    productName: j['productName'] as String? ?? '',
    quantity: (j['quantity'] as num?)?.toInt() ?? 0,
    unitCost: (j['unitCost'] as num?)?.toDouble() ?? 0,
    discount: (j['discount'] as num?)?.toDouble() ?? 0,
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
  );
}

final class PurchaseDetail {
  final String id;
  final String purchaseNumber;
  final String supplierId;
  final String supplierName;
  final String createdAt;
  final String? purchaseDate;
  final String? invoiceReference;
  final String status;
  final double subtotal;
  final double tax;
  final double discount;
  final double total;
  final String? notes;
  final List<PurchaseDetailItem> details;

  const PurchaseDetail({
    required this.id, required this.purchaseNumber, required this.supplierId,
    required this.supplierName, required this.createdAt, this.purchaseDate,
    this.invoiceReference, required this.status, required this.subtotal,
    required this.tax, required this.discount, required this.total,
    this.notes, required this.details,
  });

  factory PurchaseDetail.fromJson(Map<String, dynamic> j) => PurchaseDetail(
    id: j['id'] as String,
    purchaseNumber: j['purchaseNumber'] as String? ?? '',
    supplierId: j['supplierId'] as String? ?? '',
    supplierName: j['supplierName'] as String? ?? '',
    createdAt: j['createdAt'] as String? ?? '',
    purchaseDate: j['purchaseDate'] as String?,
    invoiceReference: j['invoiceReference'] as String?,
    status: j['status'] as String? ?? '',
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
    tax: (j['tax'] as num?)?.toDouble() ?? 0,
    discount: (j['discount'] as num?)?.toDouble() ?? 0,
    total: (j['total'] as num?)?.toDouble() ?? 0,
    notes: j['notes'] as String?,
    details: (j['details'] as List?)?.map((e) => PurchaseDetailItem.fromJson(e)).toList() ?? [],
  );
}

final class PurchaseState {
  final List<PurchaseItem> items;
  final bool loading;
  final String? error;
  const PurchaseState({this.items = const [], this.loading = false, this.error});
  PurchaseState copyWith({List<PurchaseItem>? items, bool? loading, String? error}) =>
    PurchaseState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class PurchaseNotifier extends Notifier<PurchaseState> {
  @override
  PurchaseState build() => const PurchaseState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('purchases');
      final data = r.data;
      state = PurchaseState(items: (data['items'] as List).map((e) => PurchaseItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar compras', loading: false);
    }
  }
}

final purchaseProvider = NotifierProvider<PurchaseNotifier, PurchaseState>(PurchaseNotifier.new);
