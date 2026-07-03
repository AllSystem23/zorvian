import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class BudgetDetailItem {
  final String id;
  final String budgetId;
  final String accountId;
  final String accountName;
  final String accountCode;
  final String? costCenterId;
  final String? costCenterName;
  final double budgetedAmount;
  final String? description;
  final int month;
  final int year;

  const BudgetDetailItem({
    required this.id, required this.budgetId, required this.accountId,
    required this.accountName, required this.accountCode,
    this.costCenterId, this.costCenterName, required this.budgetedAmount,
    this.description, required this.month, required this.year,
  });

  factory BudgetDetailItem.fromJson(Map<String, dynamic> j) => BudgetDetailItem(
    id: j['id'] as String? ?? '',
    budgetId: j['budgetId'] as String? ?? '',
    accountId: j['accountId'] as String? ?? '',
    accountName: j['accountName'] as String? ?? '',
    accountCode: j['accountCode'] as String? ?? '',
    costCenterId: j['costCenterId'] as String?,
    costCenterName: j['costCenterName'] as String?,
    budgetedAmount: (j['budgetedAmount'] as num?)?.toDouble() ?? 0,
    description: j['description'] as String?,
    month: j['month'] as int? ?? 0,
    year: j['year'] as int? ?? 0,
  );
}

final class BudgetTrackingItem {
  final String id;
  final String budgetDetailId;
  final String accountName;
  final String accountCode;
  final double budgetedAmount;
  final double actualAmount;
  final double variance;
  final double variancePercentage;
  final int month;
  final int year;
  final String? sourceReference;
  final String? notes;

  const BudgetTrackingItem({
    required this.id, required this.budgetDetailId, required this.accountName,
    required this.accountCode, required this.budgetedAmount, required this.actualAmount,
    required this.variance, required this.variancePercentage,
    required this.month, required this.year, this.sourceReference, this.notes,
  });

  factory BudgetTrackingItem.fromJson(Map<String, dynamic> j) => BudgetTrackingItem(
    id: j['id'] as String? ?? '',
    budgetDetailId: j['budgetDetailId'] as String? ?? '',
    accountName: j['accountName'] as String? ?? '',
    accountCode: j['accountCode'] as String? ?? '',
    budgetedAmount: (j['budgetedAmount'] as num?)?.toDouble() ?? 0,
    actualAmount: (j['actualAmount'] as num?)?.toDouble() ?? 0,
    variance: (j['variance'] as num?)?.toDouble() ?? 0,
    variancePercentage: (j['variancePercentage'] as num?)?.toDouble() ?? 0,
    month: j['month'] as int? ?? 0,
    year: j['year'] as int? ?? 0,
    sourceReference: j['sourceReference'] as String?,
    notes: j['notes'] as String?,
  );
}

final class BudgetDetailState {
  final List<BudgetDetailItem> details;
  final List<BudgetTrackingItem> trackingItems;
  final bool loading;
  final String? error;

  const BudgetDetailState({
    this.details = const [], this.trackingItems = const [],
    this.loading = false, this.error,
  });

  BudgetDetailState copyWith({
    List<BudgetDetailItem>? details, List<BudgetTrackingItem>? trackingItems,
    bool? loading, String? error,
  }) => BudgetDetailState(
    details: details ?? this.details, trackingItems: trackingItems ?? this.trackingItems,
    loading: loading ?? this.loading, error: error,
  );
}

final class BudgetDetailNotifier extends Notifier<BudgetDetailState> {
  @override
  BudgetDetailState build() => const BudgetDetailState();

  Future<void> loadByBudgetId(String budgetId) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('budget-details/by-budget/$budgetId');
      final data = r.data as List;
      state = BudgetDetailState(
        details: data.map((e) => BudgetDetailItem.fromJson(e)).toList(),
      );
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar detalles', loading: false);
    }
  }

  Future<void> loadByPeriod(int year, int month) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('budget-details/by-period', params: {'year': year, 'month': month});
      final data = r.data as List;
      state = BudgetDetailState(
        details: data.map((e) => BudgetDetailItem.fromJson(e)).toList(),
      );
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar detalles', loading: false);
    }
  }

  Future<void> loadTracking({String? budgetDetailId, int? year, int? month}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{'page': 1, 'pageSize': 50};
      if (budgetDetailId != null) params['budgetDetailId'] = budgetDetailId;
      if (year != null) params['year'] = year;
      if (month != null) params['month'] = month;
      final r = await dio.get('budget-tracking', params: params);
      final data = r.data;
      final items = (data['items'] as List).map((e) => BudgetTrackingItem.fromJson(e)).toList();
      state = state.copyWith(trackingItems: items, loading: false);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar tracking', loading: false);
    }
  }
}

final budgetDetailProvider = NotifierProvider<BudgetDetailNotifier, BudgetDetailState>(BudgetDetailNotifier.new);
