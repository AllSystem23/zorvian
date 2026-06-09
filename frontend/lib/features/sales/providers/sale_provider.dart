import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class SaleItem {
  final String id;
  final String invoiceNumber;
  final String clientName;
  final String saleDate;
  final String saleType;
  final double total;
  final double balance;
  final String status;
  final String currencyCode;

  const SaleItem({
    required this.id, required this.invoiceNumber, required this.clientName,
    required this.saleDate, required this.saleType, required this.total,
    required this.balance, required this.status, this.currencyCode = 'NIO',
  });

  factory SaleItem.fromJson(Map<String, dynamic> j) => SaleItem(
    id: j['id'] as String? ?? '',
    invoiceNumber: j['invoiceNumber'] as String? ?? '',
    clientName: j['clientName'] as String? ?? '',
    saleDate: j['saleDate'] as String? ?? '',
    saleType: j['saleType'] as String? ?? '',
    total: (j['total'] as num?)?.toDouble() ?? 0,
    balance: (j['balance'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? '',
    currencyCode: j['currencyCode'] as String? ?? 'NIO',
  );
}

final class SaleDetailItem {
  final String productName;
  final int quantity;
  final double unitPrice;
  final double discount;
  final double subtotal;

  const SaleDetailItem({
    required this.productName, required this.quantity, required this.unitPrice,
    required this.discount, required this.subtotal,
  });

  factory SaleDetailItem.fromJson(Map<String, dynamic> j) => SaleDetailItem(
    productName: j['productName'] as String? ?? '',
    quantity: j['quantity'] as int? ?? 0,
    unitPrice: (j['unitPrice'] as num?)?.toDouble() ?? 0,
    discount: (j['discount'] as num?)?.toDouble() ?? 0,
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
  );
}

final class SaleDetail {
  final String id;
  final String invoiceNumber;
  final String clientName;
  final String saleDate;
  final String saleType;
  final double subtotal;
  final double tax;
  final double discount;
  final double total;
  final double paidAmount;
  final double balance;
  final String status;
  final String? notes;
  final String currencyCode;
  final List<SaleDetailItem> details;
  final String? creditId;

  const SaleDetail({
    required this.id, required this.invoiceNumber, required this.clientName,
    required this.saleDate, required this.saleType, required this.subtotal,
    required this.tax, required this.discount, required this.total,
    required this.paidAmount, required this.balance, required this.status,
    this.notes, this.currencyCode = 'NIO', required this.details, this.creditId,
  });

  factory SaleDetail.fromJson(Map<String, dynamic> j) => SaleDetail(
    id: j['id'] as String? ?? '',
    invoiceNumber: j['invoiceNumber'] as String? ?? '',
    clientName: j['clientName'] as String? ?? '',
    saleDate: j['saleDate'] as String? ?? '',
    saleType: j['saleType'] as String? ?? '',
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
    tax: (j['tax'] as num?)?.toDouble() ?? 0,
    discount: (j['discount'] as num?)?.toDouble() ?? 0,
    total: (j['total'] as num?)?.toDouble() ?? 0,
    paidAmount: (j['paidAmount'] as num?)?.toDouble() ?? 0,
    balance: (j['balance'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? '',
    notes: j['notes'] as String?,
    currencyCode: j['currencyCode'] as String? ?? 'NIO',
    details: (j['details'] as List?)?.map((e) => SaleDetailItem.fromJson(e as Map<String, dynamic>)).toList() ?? [],
    creditId: j['creditId'] as String?,
  );
}

final class SaleState {
  final List<SaleItem> items;
  final bool loading;
  final String? error;
  const SaleState({this.items = const [], this.loading = false, this.error});
  SaleState copyWith({List<SaleItem>? items, bool? loading, String? error}) =>
    SaleState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class SaleNotifier extends Notifier<SaleState> {
  @override
  SaleState build() => const SaleState();

  Future<void> load({String? search}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{};
      if (search != null && search.isNotEmpty) params['search'] = search;
      final r = await dio.get('sales', params: params);
      final body = r.data;
      final list = body is Map ? (body['items'] ?? []) : (body is List ? body : []);
      state = SaleState(items: (list as List).map((e) => SaleItem.fromJson(e as Map<String, dynamic>)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar ventas', loading: false);
    }
  }
}

final saleProvider = NotifierProvider<SaleNotifier, SaleState>(SaleNotifier.new);
