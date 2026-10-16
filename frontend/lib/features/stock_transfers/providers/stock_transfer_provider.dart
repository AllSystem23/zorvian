import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/network/dio_client.dart';
import '../../../core/providers/company_branch_provider.dart';

final class StockTransferItem {
  final String id;
  final String transferNumber;
  final String fromBranchName;
  final String toBranchName;
  final String fromWarehouseName;
  final String toWarehouseName;
  final String status;
  final String createdAt;

  const StockTransferItem({
    required this.id, required this.transferNumber,
    this.fromBranchName = '', this.toBranchName = '',
    this.fromWarehouseName = '', this.toWarehouseName = '',
    required this.status, required this.createdAt,
  });

  factory StockTransferItem.fromJson(Map<String, dynamic> j) => StockTransferItem(
    id: j['id'] as String,
    transferNumber: j['transferNumber'] as String? ?? '',
    fromBranchName: j['fromBranchName'] as String? ?? '',
    toBranchName: j['toBranchName'] as String? ?? '',
    fromWarehouseName: j['fromWarehouseName'] as String? ?? '',
    toWarehouseName: j['toWarehouseName'] as String? ?? '',
    status: j['status'] as String? ?? '',
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class StockTransferDetailItem {
  final String id;
  final String productId;
  final String productName;
  final String productCode;
  final int quantity;
  final int quantityReceived;
  final double unitCost;

  const StockTransferDetailItem({
    required this.id, required this.productId, this.productName = '', this.productCode = '',
    required this.quantity, this.quantityReceived = 0, this.unitCost = 0,
  });

  factory StockTransferDetailItem.fromJson(Map<String, dynamic> j) => StockTransferDetailItem(
    id: j['id'] as String? ?? '',
    productId: j['productId'] as String? ?? '',
    productName: j['productName'] as String? ?? '',
    productCode: j['productCode'] as String? ?? '',
    quantity: (j['quantity'] as num?)?.toInt() ?? 0,
    quantityReceived: (j['quantityReceived'] as num?)?.toInt() ?? 0,
    unitCost: (j['unitCost'] as num?)?.toDouble() ?? 0,
  );
}

final class StockTransferDetail {
  final String id;
  final String transferNumber;
  final String fromBranchId;
  final String fromBranchName;
  final String fromWarehouseId;
  final String fromWarehouseName;
  final String toBranchId;
  final String toBranchName;
  final String toWarehouseId;
  final String toWarehouseName;
  final String status;
  final String? dispatchedAt;
  final String? receivedAt;
  final String? notes;
  final String requestedBy;
  final String createdAt;
  final List<StockTransferDetailItem> details;

  const StockTransferDetail({
    required this.id, required this.transferNumber,
    required this.fromBranchId, this.fromBranchName = '',
    this.fromWarehouseId = '', this.fromWarehouseName = '',
    required this.toBranchId, this.toBranchName = '',
    this.toWarehouseId = '', this.toWarehouseName = '',
    required this.status, this.dispatchedAt, this.receivedAt, this.notes,
    required this.requestedBy, required this.createdAt, required this.details,
  });

  factory StockTransferDetail.fromJson(Map<String, dynamic> j) => StockTransferDetail(
    id: j['id'] as String,
    transferNumber: j['transferNumber'] as String? ?? '',
    fromBranchId: j['fromBranchId'] as String? ?? '',
    fromBranchName: j['fromBranchName'] as String? ?? '',
    fromWarehouseId: j['fromWarehouseId'] as String? ?? '',
    fromWarehouseName: j['fromWarehouseName'] as String? ?? '',
    toBranchId: j['toBranchId'] as String? ?? '',
    toBranchName: j['toBranchName'] as String? ?? '',
    toWarehouseId: j['toWarehouseId'] as String? ?? '',
    toWarehouseName: j['toWarehouseName'] as String? ?? '',
    status: j['status'] as String? ?? '',
    dispatchedAt: j['dispatchedAt'] as String?,
    receivedAt: j['receivedAt'] as String?,
    notes: j['notes'] as String?,
    requestedBy: j['requestedBy'] as String? ?? '',
    createdAt: j['createdAt'] as String? ?? '',
    details: (j['details'] as List?)?.map((e) => StockTransferDetailItem.fromJson(e)).toList() ?? [],
  );
}

final class CreateStockTransferRequest {
  final String? fromWarehouseId;
  final String? toWarehouseId;
  final String? notes;
  final List<CreateStockTransferLine> details;

  const CreateStockTransferRequest({
    this.fromWarehouseId, this.toWarehouseId, this.notes, required this.details,
  });

  Map<String, dynamic> toJson() => {
    'fromWarehouseId': fromWarehouseId,
    'toWarehouseId': toWarehouseId,
    'notes': notes,
    'details': details.map((l) => l.toJson()).toList(),
  };
}

final class CreateStockTransferLine {
  final String productId;
  final int quantity;

  const CreateStockTransferLine({required this.productId, required this.quantity});

  Map<String, dynamic> toJson() => {
    'productId': productId,
    'quantity': quantity,
  };
}

final class ReceiveStockTransferLine {
  final String detailId;
  final int quantity;

  const ReceiveStockTransferLine({required this.detailId, required this.quantity});

  Map<String, dynamic> toJson() => {
    'detailId': detailId,
    'quantity': quantity,
  };
}

final class ReceiveStockTransferRequest {
  final List<ReceiveStockTransferLine> lines;

  const ReceiveStockTransferRequest({required this.lines});

  Map<String, dynamic> toJson() => {
    'lines': lines.map((l) => l.toJson()).toList(),
  };
}

/// POST stock-transfers/{id}/receive. Body vacío (o líneas null) = recepción
/// TOTAL de todo lo pendiente; líneas con detailId+quantity = recepción PARCIAL.
Future<Map<String, dynamic>> receiveStockTransfer(
  DioClient dio,
  String id, {
  List<ReceiveStockTransferLine>? lines,
}) async {
  final body = lines == null ? null : ReceiveStockTransferRequest(lines: lines);
  final r = await dio.post(
    'stock-transfers/$id/receive',
    data: body?.toJson() ?? <String, dynamic>{},
  );
  return Map<String, dynamic>.from(r.data as Map);
}

final class StockTransferNotifier extends AsyncNotifier<List<StockTransferItem>> {
  @override
  Future<List<StockTransferItem>> build() async {
    ref.watch(branchScopeProvider);
    return _fetch();
  }

  Future<void> load({String? search}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => _fetch(search: search));
  }

  Future<List<StockTransferItem>> _fetch({String? search}) async {
    final dio = ref.read(dioClientProvider);
    final params = <String, dynamic>{};
    if (search != null && search.isNotEmpty) params['search'] = search;
    final r = await dio.get('stock-transfers', params: params);
    final data = r.data;
    return (data['items'] as List).map((e) => StockTransferItem.fromJson(e)).toList();
  }
}

final stockTransferProvider =
    AsyncNotifierProvider<StockTransferNotifier, List<StockTransferItem>>(StockTransferNotifier.new);
