import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';

final class PurchaseRequestItem {
  final String id;
  final String requestNumber;
  final String title;
  final String priority;
  final String status;
  final double estimatedTotal;
  final String createdAt;
  final String? branchName;

  const PurchaseRequestItem({
    required this.id, required this.requestNumber, required this.title,
    required this.priority, required this.status, required this.estimatedTotal,
    required this.createdAt, this.branchName,
  });

  factory PurchaseRequestItem.fromJson(Map<String, dynamic> j) => PurchaseRequestItem(
    id: j['id'] as String,
    requestNumber: j['requestNumber'] as String? ?? '',
    title: j['title'] as String? ?? '',
    priority: j['priority'] as String? ?? 'normal',
    status: j['status'] as String? ?? '',
    estimatedTotal: (j['estimatedTotal'] as num?)?.toDouble() ?? 0,
    createdAt: j['createdAt'] as String? ?? '',
    branchName: j['branchName'] as String?,
  );
}

final class PurchaseRequestDetailItem {
  final String productId;
  final String productName;
  final String productCode;
  final int quantity;
  final double estimatedUnitPrice;
  final double subtotal;
  final String? notes;

  const PurchaseRequestDetailItem({
    required this.productId, this.productName = '', this.productCode = '',
    required this.quantity, required this.estimatedUnitPrice,
    required this.subtotal, this.notes,
  });

  factory PurchaseRequestDetailItem.fromJson(Map<String, dynamic> j) => PurchaseRequestDetailItem(
    productId: j['productId'] as String? ?? '',
    productName: j['productName'] as String? ?? '',
    productCode: j['productCode'] as String? ?? '',
    quantity: (j['quantity'] as num?)?.toInt() ?? 0,
    estimatedUnitPrice: (j['estimatedUnitPrice'] as num?)?.toDouble() ?? 0,
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
    notes: j['notes'] as String?,
  );
}

final class PurchaseRequestDetail {
  final String id;
  final String requestNumber;
  final String title;
  final String priority;
  final String? neededByDate;
  final String status;
  final String? rejectionReason;
  final double estimatedTotal;
  final String? notes;
  final String branchId;
  final String? branchName;
  final String requestedBy;
  final String createdAt;
  final List<PurchaseRequestDetailItem> details;

  const PurchaseRequestDetail({
    required this.id, required this.requestNumber, required this.title,
    required this.priority, this.neededByDate, required this.status,
    this.rejectionReason, required this.estimatedTotal, this.notes,
    required this.branchId, this.branchName, required this.requestedBy,
    required this.createdAt, required this.details,
  });

  factory PurchaseRequestDetail.fromJson(Map<String, dynamic> j) => PurchaseRequestDetail(
    id: j['id'] as String,
    requestNumber: j['requestNumber'] as String? ?? '',
    title: j['title'] as String? ?? '',
    priority: j['priority'] as String? ?? 'normal',
    neededByDate: j['neededByDate'] as String?,
    status: j['status'] as String? ?? '',
    rejectionReason: j['rejectionReason'] as String?,
    estimatedTotal: (j['estimatedTotal'] as num?)?.toDouble() ?? 0,
    notes: j['notes'] as String?,
    branchId: j['branchId'] as String? ?? '',
    branchName: j['branchName'] as String?,
    requestedBy: j['requestedBy'] as String? ?? '',
    createdAt: j['createdAt'] as String? ?? '',
    details: (j['details'] as List?)?.map((e) => PurchaseRequestDetailItem.fromJson(e)).toList() ?? [],
  );
}

final class CreatePurchaseRequestRequest {
  final String title;
  final String priority;
  final String? neededByDate;
  final String? branchId;
  final String? notes;
  final List<CreatePurchaseRequestLine> details;

  const CreatePurchaseRequestRequest({
    required this.title, required this.priority, this.neededByDate,
    this.branchId, this.notes, required this.details,
  });

  Map<String, dynamic> toJson() => {
    'title': title,
    'priority': priority,
    'neededByDate': neededByDate,
    'branchId': branchId,
    'notes': notes,
    'details': details.map((l) => l.toJson()).toList(),
  };
}

final class CreatePurchaseRequestLine {
  final String productId;
  final int quantity;
  final double estimatedUnitPrice;

  const CreatePurchaseRequestLine({
    required this.productId, required this.quantity,
    required this.estimatedUnitPrice,
  });

  Map<String, dynamic> toJson() => {
    'productId': productId,
    'quantity': quantity,
    'estimatedUnitPrice': estimatedUnitPrice,
    'notes': null,
  };
}

final class PurchaseRequestNotifier extends AsyncNotifier<List<PurchaseRequestItem>> {
  @override
  Future<List<PurchaseRequestItem>> build() async {
    ref.watch(branchScopeProvider);
    return _fetch();
  }

  Future<void> load({String? search}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => _fetch(search: search));
  }

  Future<List<PurchaseRequestItem>> _fetch({String? search}) async {
    final dio = ref.read(dioClientProvider);
    final params = <String, dynamic>{};
    if (search != null && search.isNotEmpty) params['search'] = search;
    final r = await dio.get('purchase-requests', params: params);
    final data = r.data;
    return (data['items'] as List).map((e) => PurchaseRequestItem.fromJson(e)).toList();
  }
}

final purchaseRequestProvider =
    AsyncNotifierProvider<PurchaseRequestNotifier, List<PurchaseRequestItem>>(PurchaseRequestNotifier.new);
