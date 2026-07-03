import 'dart:async';
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
    id: (j['id'] ?? '').toString(),
    invoiceNumber: (j['invoiceNumber'] ?? '').toString(),
    clientName: (j['clientName'] ?? '').toString(),
    saleDate: (j['saleDate'] ?? '').toString(),
    saleType: (j['saleType'] ?? '').toString(),
    total: (j['total'] as num?)?.toDouble() ?? 0,
    balance: (j['balance'] as num?)?.toDouble() ?? 0,
    status: (j['status'] ?? '').toString(),
    currencyCode: (j['currencyCode'] ?? 'NIO').toString(),
  );

  bool get isCancellable => status == 'completed' || status == 'pending';
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
    productName: (j['productName'] ?? '').toString(),
    quantity: (j['quantity'] as num?)?.toInt() ?? 0,
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
    id: (j['id'] ?? '').toString(),
    invoiceNumber: (j['invoiceNumber'] ?? '').toString(),
    clientName: (j['clientName'] ?? '').toString(),
    saleDate: (j['saleDate'] ?? '').toString(),
    saleType: (j['saleType'] ?? '').toString(),
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
    tax: (j['tax'] as num?)?.toDouble() ?? 0,
    discount: (j['discount'] as num?)?.toDouble() ?? 0,
    total: (j['total'] as num?)?.toDouble() ?? 0,
    paidAmount: (j['paidAmount'] as num?)?.toDouble() ?? 0,
    balance: (j['balance'] as num?)?.toDouble() ?? 0,
    status: (j['status'] ?? '').toString(),
    notes: j['notes'] as String?,
    currencyCode: (j['currencyCode'] ?? 'NIO').toString(),
    details: (j['details'] as List?)
        ?.map((e) => SaleDetailItem.fromJson(e as Map<String, dynamic>))
        .toList() ?? [],
    creditId: (j['creditId'] as String?),
  );

  bool get isCancellable => status == 'completed' || status == 'pending';
}

final class SaleState {
  final List<SaleItem> items;
  final int total;
  final int page;
  final int pageSize;
  final String? search;
  final bool loading;
  final String? error;

  const SaleState({
    this.items = const [],
    this.total = 0,
    this.page = 1,
    this.pageSize = 20,
    this.search,
    this.loading = false,
    this.error,
  });

  SaleState copyWith({
    List<SaleItem>? items,
    int? total,
    int? page,
    int? pageSize,
    String? search,
    bool? loading,
    String? error,
  }) => SaleState(
    items: items ?? this.items,
    total: total ?? this.total,
    page: page ?? this.page,
    pageSize: pageSize ?? this.pageSize,
    search: search ?? this.search,
    loading: loading ?? this.loading,
    error: error,
  );
}

final class SaleNotifier extends Notifier<SaleState> {
  @override
  SaleState build() => const SaleState();

  Future<void> load({String? search, int? page}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{
        'page': page ?? state.page,
        'pageSize': state.pageSize,
      };
      if (search != null && search.isNotEmpty) params['search'] = search;

      final r = await dio.get('sales', params: params);
      final body = r.data as Map<String, dynamic>;
      final items = (body['items'] as List?)
              ?.map((e) => SaleItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [];

      state = state.copyWith(
        items: items,
        total: (body['total'] as num?)?.toInt() ?? items.length,
        page: (body['page'] as num?)?.toInt() ?? (page ?? state.page),
        pageSize: (body['pageSize'] as num?)?.toInt() ?? state.pageSize,
        search: search,
        loading: false,
      );
    } catch (e) {
      state = state.copyWith(loading: false, error: e.toString());
    }
  }
}

final saleProvider = NotifierProvider<SaleNotifier, SaleState>(SaleNotifier.new);