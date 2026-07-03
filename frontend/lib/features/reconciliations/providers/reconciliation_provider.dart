import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class ReconciliationItem {
  final String id;
  final String bankAccountName;
  final String dateFrom;
  final String dateTo;
  final String status;
  final int totalTransactions;
  final int matchedCount;
  final int unmatchedCount;
  final double difference;
  final String createdAt;

  const ReconciliationItem({
    required this.id, required this.bankAccountName,
    required this.dateFrom, required this.dateTo,
    required this.status, required this.totalTransactions,
    required this.matchedCount, required this.unmatchedCount,
    required this.difference, required this.createdAt,
  });

  factory ReconciliationItem.fromJson(Map<String, dynamic> j) => ReconciliationItem(
    id: j['id'] as String? ?? '',
    bankAccountName: j['bankAccountName'] as String? ?? '',
    dateFrom: j['dateFrom'] as String? ?? '',
    dateTo: j['dateTo'] as String? ?? '',
    status: j['status'] as String? ?? '',
    totalTransactions: j['totalTransactions'] as int? ?? 0,
    matchedCount: j['matchedCount'] as int? ?? 0,
    unmatchedCount: j['unmatchedCount'] as int? ?? 0,
    difference: (j['difference'] as num?)?.toDouble() ?? 0,
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class ReconciliationDetailItem {
  final String id;
  final String reference;
  final double amount;
  final String transactionType;
  final String transactionDate;
  final String? description;
  final String sourceType;
  final String matchStatus;
  final String? notes;

  const ReconciliationDetailItem({
    required this.id, required this.reference, required this.amount,
    required this.transactionType, required this.transactionDate,
    this.description, required this.sourceType, required this.matchStatus, this.notes,
  });

  factory ReconciliationDetailItem.fromJson(Map<String, dynamic> j) => ReconciliationDetailItem(
    id: j['id'] as String? ?? '',
    reference: j['reference'] as String? ?? '',
    amount: (j['amount'] as num?)?.toDouble() ?? 0,
    transactionType: j['transactionType'] as String? ?? '',
    transactionDate: j['transactionDate'] as String? ?? '',
    description: j['description'] as String?,
    sourceType: j['sourceType'] as String? ?? '',
    matchStatus: j['matchStatus'] as String? ?? '',
    notes: j['notes'] as String?,
  );
}

final class ReconciliationState {
  final List<ReconciliationItem> items;
  final ReconciliationDetailItem? selectedReconciliation;
  final List<ReconciliationDetailItem> details;
  final bool loading;
  final String? error;
  final int total;
  final int page;

  const ReconciliationState({
    this.items = const [], this.selectedReconciliation, this.details = const [],
    this.loading = false, this.error, this.total = 0, this.page = 1,
  });

  ReconciliationState copyWith({
    List<ReconciliationItem>? items, ReconciliationDetailItem? selectedReconciliation,
    List<ReconciliationDetailItem>? details, bool? loading, String? error,
    int? total, int? page,
  }) => ReconciliationState(
    items: items ?? this.items, selectedReconciliation: selectedReconciliation ?? this.selectedReconciliation,
    details: details ?? this.details, loading: loading ?? this.loading, error: error,
    total: total ?? this.total, page: page ?? this.page,
  );
}

final class ReconciliationNotifier extends Notifier<ReconciliationState> {
  @override
  ReconciliationState build() => const ReconciliationState();

  Future<void> load({String? bankAccountId, String? status, int? page}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{'page': page ?? state.page, 'pageSize': 20};
      if (bankAccountId != null) params['bankAccountId'] = bankAccountId;
      if (status != null) params['status'] = status;
      final r = await dio.get('reconciliations', params: params);
      final data = r.data;
      final items = (data['items'] as List).map((e) => ReconciliationItem.fromJson(e)).toList();
      state = state.copyWith(items: items, total: data['total'] as int? ?? 0, loading: false, page: page ?? state.page);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar conciliaciones', loading: false);
    }
  }

  Future<void> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('reconciliations/$id');
      await load();
    } catch (_) {
      state = state.copyWith(error: 'Error al eliminar conciliación');
    }
  }

  Future<void> importBankStatement(String id, String filePath) async {
    state = state.copyWith(loading: true);
    try {
      final dio = ref.read(dioClientProvider);
      final form = FormData.fromMap({'file': await MultipartFile.fromFile(filePath)});
      await dio.post('reconciliations/$id/import', data: form);
      await load();
    } catch (_) {
      state = state.copyWith(error: 'Error al importar estado de cuenta', loading: false);
    }
  }

  Future<void> runAutoMatch(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('reconciliations/$id/auto-match');
      await load();
    } catch (_) {
      state = state.copyWith(error: 'Error al ejecutar matching automático');
    }
  }
}

final reconciliationProvider = NotifierProvider<ReconciliationNotifier, ReconciliationState>(ReconciliationNotifier.new);
