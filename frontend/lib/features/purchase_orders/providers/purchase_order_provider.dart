import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class PurchaseOrderItem {
  final String id;
  final String orderNumber;
  final String supplierName;
  final String createdAt;
  final String status;
  final double total;
  final String currencyCode;

  const PurchaseOrderItem({
    required this.id, required this.orderNumber, required this.supplierName,
    required this.createdAt, required this.status, required this.total, this.currencyCode = 'NIO',
  });

  factory PurchaseOrderItem.fromJson(Map<String, dynamic> j) => PurchaseOrderItem(
    id: j['id'] as String,
    orderNumber: j['orderNumber'] as String? ?? '',
    supplierName: j['supplierName'] as String? ?? '',
    createdAt: j['createdAt'] as String? ?? '',
    status: j['status'] as String? ?? '',
    total: (j['total'] as num?)?.toDouble() ?? 0,
    currencyCode: j['currencyCode'] as String? ?? 'NIO',
  );
}

final class PurchaseOrderDetailItem {
  final String productId;
  final String productName;
  final int quantity;
  final int quantityReceived;
  final double unitCost;
  final double discount;
  final double subtotal;

  const PurchaseOrderDetailItem({
    required this.productId, required this.productName, required this.quantity,
    required this.quantityReceived, required this.unitCost, required this.discount,
    required this.subtotal,
  });

  factory PurchaseOrderDetailItem.fromJson(Map<String, dynamic> j) => PurchaseOrderDetailItem(
    productId: j['productId'] as String,
    productName: j['productName'] as String? ?? '',
    quantity: (j['quantity'] as num?)?.toInt() ?? 0,
    quantityReceived: (j['quantityReceived'] as num?)?.toInt() ?? 0,
    unitCost: (j['unitCost'] as num?)?.toDouble() ?? 0,
    discount: (j['discount'] as num?)?.toDouble() ?? 0,
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
  );
}

final class PurchaseOrderDetail {
  final String id;
  final String orderNumber;
  final String supplierId;
  final String supplierName;
  final String createdAt;
  final String status;
  final double subtotal;
  final double tax;
  final double discount;
  final double total;
  final String? notes;
  final String currencyCode;
  final List<PurchaseOrderDetailItem> details;
  final String? purchaseOrderId;

  const PurchaseOrderDetail({
    required this.id, required this.orderNumber, required this.supplierId,
    required this.supplierName, required this.createdAt, required this.status,
    required this.subtotal, required this.tax, required this.discount,
    required this.total, this.notes, this.currencyCode = 'NIO',
    required this.details, this.purchaseOrderId,
  });

  factory PurchaseOrderDetail.fromJson(Map<String, dynamic> j) => PurchaseOrderDetail(
    id: j['id'] as String,
    orderNumber: j['orderNumber'] as String? ?? '',
    supplierId: j['supplierId'] as String? ?? '',
    supplierName: j['supplierName'] as String? ?? '',
    createdAt: j['createdAt'] as String? ?? '',
    status: j['status'] as String? ?? '',
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
    tax: (j['tax'] as num?)?.toDouble() ?? 0,
    discount: (j['discount'] as num?)?.toDouble() ?? 0,
    total: (j['total'] as num?)?.toDouble() ?? 0,
    notes: j['notes'] as String?,
    currencyCode: j['currencyCode'] as String? ?? 'NIO',
    details: (j['details'] as List?)?.map((e) => PurchaseOrderDetailItem.fromJson(e)).toList() ?? [],
    purchaseOrderId: j['purchaseOrderId'] as String?,
  );
}

final class CreatePurchaseOrderRequest {
  final String supplierId;
  final String? notes;
  final String currencyCode;
  final List<CreatePurchaseOrderLine> lines;

  const CreatePurchaseOrderRequest({
    required this.supplierId, this.notes, this.currencyCode = 'NIO', required this.lines,
  });

  Map<String, dynamic> toJson() => {
    'supplierId': supplierId,
    'notes': notes,
    'currencyCode': currencyCode,
    'lines': lines.map((l) => l.toJson()).toList(),
  };
}

final class CreatePurchaseOrderLine {
  final String productId;
  final int quantity;
  final double unitCost;
  final double discount;

  const CreatePurchaseOrderLine({
    required this.productId, required this.quantity,
    required this.unitCost, this.discount = 0,
  });

  Map<String, dynamic> toJson() => {
    'productId': productId,
    'quantity': quantity,
    'unitCost': unitCost,
    'discount': discount,
  };
}

final class ReceivePurchaseOrderRequest {
  final List<ReceivePurchaseOrderLine> lines;

  const ReceivePurchaseOrderRequest({required this.lines});

  Map<String, dynamic> toJson() => {
    'lines': lines.map((l) => l.toJson()).toList(),
  };
}

final class ReceivePurchaseOrderLine {
  final String productId;
  final int quantity;
  final double unitCost;

  const ReceivePurchaseOrderLine({
    required this.productId, required this.quantity, required this.unitCost,
  });

  Map<String, dynamic> toJson() => {
    'productId': productId,
    'quantity': quantity,
    'unitCost': unitCost,
  };
}

final class PurchaseOrderNotifier extends AsyncNotifier<List<PurchaseOrderItem>> {
  @override
  Future<List<PurchaseOrderItem>> build() async {
    return _fetch();
  }

  Future<void> load({String? search}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => _fetch(search: search));
  }

  Future<List<PurchaseOrderItem>> _fetch({String? search}) async {
    final dio = ref.read(dioClientProvider);
    final params = <String, dynamic>{};
    if (search != null && search.isNotEmpty) params['search'] = search;
    final r = await dio.get('purchase-orders', params: params);
    final data = r.data;
    return (data['items'] as List).map((e) => PurchaseOrderItem.fromJson(e)).toList();
  }
}

final purchaseOrderProvider = AsyncNotifierProvider<PurchaseOrderNotifier, List<PurchaseOrderItem>>(PurchaseOrderNotifier.new);
