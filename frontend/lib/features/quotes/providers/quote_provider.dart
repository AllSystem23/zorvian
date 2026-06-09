import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class QuoteItem {
  final String id;
  final String quoteNumber;
  final String clientName;
  final double total;
  final String status;
  final String quoteDate;

  const QuoteItem({
    required this.id, required this.quoteNumber, required this.clientName,
    required this.total, required this.status, required this.quoteDate,
  });

  factory QuoteItem.fromJson(Map<String, dynamic> j) => QuoteItem(
    id: j['id'] as String,
    quoteNumber: j['quoteNumber'] as String? ?? '',
    clientName: j['clientName'] as String? ?? '',
    total: (j['total'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? '',
    quoteDate: j['quoteDate'] as String? ?? '',
  );
}

final class QuoteDetailItem {
  final String productId;
  final String productName;
  final int quantity;
  final double unitPrice;
  final double discount;
  final double subtotal;

  const QuoteDetailItem({
    required this.productId, required this.productName,
    required this.quantity, required this.unitPrice,
    required this.discount, required this.subtotal,
  });

  factory QuoteDetailItem.fromJson(Map<String, dynamic> j) => QuoteDetailItem(
    productId: j['productId'] as String,
    productName: j['productName'] as String? ?? '',
    quantity: (j['quantity'] as num?)?.toInt() ?? 0,
    unitPrice: (j['unitPrice'] as num?)?.toDouble() ?? 0,
    discount: (j['discount'] as num?)?.toDouble() ?? 0,
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
  );
}

final class QuoteDetail {
  final String id;
  final String quoteNumber;
  final String clientId;
  final String clientName;
  final String? employeeName;
  final String quoteDate;
  final String? expirationDate;
  final double subtotal;
  final double tax;
  final double discount;
  final double total;
  final String status;
  final String? notes;
  final List<QuoteDetailItem> details;

  const QuoteDetail({
    required this.id, required this.quoteNumber,
    required this.clientId, required this.clientName,
    this.employeeName,
    required this.quoteDate, this.expirationDate,
    required this.subtotal, required this.tax,
    required this.discount, required this.total,
    required this.status, this.notes,
    required this.details,
  });

  factory QuoteDetail.fromJson(Map<String, dynamic> j) => QuoteDetail(
    id: j['id'] as String,
    quoteNumber: j['quoteNumber'] as String? ?? '',
    clientId: j['clientId'] as String,
    clientName: j['clientName'] as String? ?? '',
    employeeName: j['employeeName'] as String?,
    quoteDate: j['quoteDate'] as String? ?? '',
    expirationDate: j['expirationDate'] as String?,
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
    tax: (j['tax'] as num?)?.toDouble() ?? 0,
    discount: (j['discount'] as num?)?.toDouble() ?? 0,
    total: (j['total'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? '',
    notes: j['notes'] as String?,
    details: (j['details'] as List?)?.map((e) => QuoteDetailItem.fromJson(e)).toList() ?? [],
  );
}

final class QuoteState {
  final List<QuoteItem> items;
  final bool loading;
  final String? error;
  const QuoteState({this.items = const [], this.loading = false, this.error});
  QuoteState copyWith({List<QuoteItem>? items, bool? loading, String? error}) =>
    QuoteState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class QuoteNotifier extends Notifier<QuoteState> {
  @override
  QuoteState build() => const QuoteState();

  Future<void> load({String? search}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{};
      if (search != null && search.isNotEmpty) params['search'] = search;
      final r = await dio.get('quotes', params: params);
      final data = r.data;
      state = QuoteState(items: (data['items'] as List).map((e) => QuoteItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar cotizaciones', loading: false);
    }
  }

  Future<void> updateStatus(String quoteId, String newStatus) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.patch('quotes/$quoteId/status', data: {'status': newStatus});
      await load();
    } catch (e) {
      // Handle error
    }
  }
}

final quoteProvider = NotifierProvider<QuoteNotifier, QuoteState>(QuoteNotifier.new);
