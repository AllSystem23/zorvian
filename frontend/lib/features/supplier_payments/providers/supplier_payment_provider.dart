import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class SupplierPaymentItem {
  final String id;
  final String purchaseId;
  final String purchaseNumber;
  final double amount;
  final String paymentMethod;
  final String? referenceNumber;
  final String paymentDate;
  final String? notes;

  const SupplierPaymentItem({
    required this.id,
    required this.purchaseId,
    required this.purchaseNumber,
    required this.amount,
    required this.paymentMethod,
    this.referenceNumber,
    required this.paymentDate,
    this.notes,
  });

  factory SupplierPaymentItem.fromJson(Map<String, dynamic> j) => SupplierPaymentItem(
    id: j['id'] as String,
    purchaseId: j['purchaseId'] as String? ?? '',
    purchaseNumber: j['purchaseNumber'] as String? ?? '',
    amount: (j['amount'] as num?)?.toDouble() ?? 0,
    paymentMethod: j['paymentMethod'] as String? ?? '',
    referenceNumber: j['referenceNumber'] as String?,
    paymentDate: j['paymentDate'] as String? ?? '',
    notes: j['notes'] as String?,
  );
}

final class SupplierPaymentState {
  final List<SupplierPaymentItem> items;
  final bool loading;
  final String? error;
  const SupplierPaymentState({this.items = const [], this.loading = false, this.error});
  SupplierPaymentState copyWith({List<SupplierPaymentItem>? items, bool? loading, String? error}) =>
    SupplierPaymentState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class SupplierPaymentNotifier extends Notifier<SupplierPaymentState> {
  @override
  SupplierPaymentState build() => const SupplierPaymentState();

  Future<void> load({String? purchaseId}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final path = purchaseId != null
          ? 'supplier-payments/by-purchase/$purchaseId'
          : 'supplier-payments';
      final r = await dio.get(path);
      final data = r.data as List;
      state = SupplierPaymentState(
        items: data.map((e) => SupplierPaymentItem.fromJson(e)).toList(),
      );
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar pagos', loading: false);
    }
  }

  Future<void> create({
    required String purchaseId,
    required double amount,
    required String paymentMethod,
    String? referenceNumber,
    String? notes,
  }) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('supplier-payments', data: {
        'purchaseId': purchaseId,
        'amount': amount,
        'paymentMethod': paymentMethod,
        if (referenceNumber != null && referenceNumber.isNotEmpty) 'referenceNumber': referenceNumber,
        if (notes != null && notes.isNotEmpty) 'notes': notes,
      });
      await load();
    } catch (_) {
      rethrow;
    }
  }
}

final supplierPaymentProvider = NotifierProvider<SupplierPaymentNotifier, SupplierPaymentState>(SupplierPaymentNotifier.new);
