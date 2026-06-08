import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class CreditNoteItem {
  final String id;
  final String creditNoteNumber;
  final String invoiceNumber;
  final String issueDate;
  final String reason;
  final double total;
  final String status;

  const CreditNoteItem({
    required this.id, required this.creditNoteNumber, required this.invoiceNumber,
    required this.issueDate, required this.reason, required this.total, required this.status,
  });

  factory CreditNoteItem.fromJson(Map<String, dynamic> j) => CreditNoteItem(
    id: j['id'] as String? ?? '',
    creditNoteNumber: j['creditNoteNumber'] as String? ?? '',
    invoiceNumber: j['invoiceNumber'] as String? ?? '',
    issueDate: j['issueDate'] as String? ?? '',
    reason: j['reason'] as String? ?? '',
    total: (j['total'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? '',
  );
}

final class CreditNoteState {
  final List<CreditNoteItem> items;
  final bool loading;
  final String? error;
  const CreditNoteState({this.items = const [], this.loading = false, this.error});
  CreditNoteState copyWith({List<CreditNoteItem>? items, bool? loading, String? error}) =>
    CreditNoteState(items: items ?? this.items, loading: loading ?? this.loading, error: error);
}

final class CreditNoteNotifier extends Notifier<CreditNoteState> {
  @override
  CreditNoteState build() => const CreditNoteState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('credit-notes');
      final data = r.data as List;
      state = CreditNoteState(items: data.map((e) => CreditNoteItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar notas de crédito', loading: false);
    }
  }
}

final creditNoteProvider = NotifierProvider<CreditNoteNotifier, CreditNoteState>(CreditNoteNotifier.new);
