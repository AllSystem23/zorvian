import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class SupplierCreditNoteItem {
  final String id;
  final String creditNoteNumber;
  final String? supplierId;
  final String? supplierName;
  final String? purchaseId;
  final String creditNoteDate;
  final String? reason;
  final double subtotal;
  final double tax;
  final double total;
  final String status;

  const SupplierCreditNoteItem({
    required this.id,
    required this.creditNoteNumber,
    this.supplierId,
    this.supplierName,
    this.purchaseId,
    required this.creditNoteDate,
    this.reason,
    required this.subtotal,
    required this.tax,
    required this.total,
    required this.status,
  });

  factory SupplierCreditNoteItem.fromJson(Map<String, dynamic> j) => SupplierCreditNoteItem(
    id: j['id'] as String,
    creditNoteNumber: j['creditNoteNumber'] as String? ?? '',
    supplierId: j['supplierId'] as String?,
    supplierName: j['supplierName'] as String?,
    purchaseId: j['purchaseId'] as String?,
    creditNoteDate: j['creditNoteDate'] as String? ?? '',
    reason: j['reason'] as String?,
    subtotal: (j['subtotal'] as num?)?.toDouble() ?? 0,
    tax: (j['tax'] as num?)?.toDouble() ?? 0,
    total: (j['total'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? 'pending',
  );
}

final class SupplierCreditNoteState {
  final List<SupplierCreditNoteItem> items;
  final bool loading;
  final String? error;
  const SupplierCreditNoteState({this.items = const [], this.loading = false, this.error});
  SupplierCreditNoteState copyWith({List<SupplierCreditNoteItem>? items, bool? loading, String? error}) =>
    SupplierCreditNoteState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class SupplierCreditNoteNotifier extends Notifier<SupplierCreditNoteState> {
  @override
  SupplierCreditNoteState build() => const SupplierCreditNoteState();

  Future<void> load({String? purchaseId}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final path = purchaseId != null
          ? 'supplier-credit-notes/by-purchase/$purchaseId'
          : 'supplier-credit-notes';
      final r = await dio.get(path);
      final data = r.data as List;
      state = SupplierCreditNoteState(
        items: data.map((e) => SupplierCreditNoteItem.fromJson(e)).toList(),
      );
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar notas de crédito', loading: false);
    }
  }

  Future<void> create({
    required String supplierId,
    String? purchaseId,
    required String creditNoteDate,
    String? reason,
    required double subtotal,
    required double tax,
    required double total,
  }) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('supplier-credit-notes', data: {
        'supplierId': supplierId,
        if (purchaseId != null) 'purchaseId': purchaseId,
        'creditNoteDate': creditNoteDate,
        if (reason != null && reason.isNotEmpty) 'reason': reason,
        'subtotal': subtotal,
        'tax': tax,
        'total': total,
      });
      await load();
    } catch (_) {
      rethrow;
    }
  }
}

final supplierCreditNoteProvider = NotifierProvider<SupplierCreditNoteNotifier, SupplierCreditNoteState>(SupplierCreditNoteNotifier.new);
