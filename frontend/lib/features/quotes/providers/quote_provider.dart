import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class QuoteItem {
  final String id;
  final String folio;
  final String clientName;
  final double total;
  final String status;
  final String createdAt;

  const QuoteItem({
    required this.id, required this.folio, required this.clientName,
    required this.total, required this.status, required this.createdAt,
  });

  factory QuoteItem.fromJson(Map<String, dynamic> j) => QuoteItem(
    id: j['id'] as String,
    folio: j['folio'] as String? ?? '',
    clientName: j['clientName'] as String? ?? '',
    total: (j['total'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? '',
    createdAt: j['createdAt'] as String? ?? '',
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

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('quotes');
      final data = r.data as List;
      state = QuoteState(items: data.map((e) => QuoteItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar cotizaciones', loading: false);
    }
  }
}

final quoteProvider = NotifierProvider<QuoteNotifier, QuoteState>(QuoteNotifier.new);
