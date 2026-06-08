import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class BudgetItem {
  final String id;
  final int year;
  final int month;
  final String accountId;
  final String accountName;
  final String accountCode;
  final String? costCenterId;
  final String? costCenterName;
  final String? costCenterCode;
  final double budgetedAmount;

  const BudgetItem({
    required this.id, required this.year, required this.month,
    required this.accountId, required this.accountName, required this.accountCode,
    this.costCenterId, this.costCenterName, this.costCenterCode,
    required this.budgetedAmount,
  });

  factory BudgetItem.fromJson(Map<String, dynamic> j) => BudgetItem(
    id: j['id'] as String? ?? '',
    year: j['year'] as int? ?? 0,
    month: j['month'] as int? ?? 0,
    accountId: j['accountId'] as String? ?? '',
    accountName: j['accountName'] as String? ?? '',
    accountCode: j['accountCode'] as String? ?? '',
    costCenterId: j['costCenterId'] as String?,
    costCenterName: j['costCenterName'] as String?,
    costCenterCode: j['costCenterCode'] as String?,
    budgetedAmount: (j['budgetedAmount'] as num?)?.toDouble() ?? 0,
  );
}

final class BudgetVsActualItem {
  final String budgetId;
  final int year;
  final int month;
  final String accountId;
  final String accountCode;
  final String accountName;
  final String? costCenterId;
  final String? costCenterName;
  final double budgetedAmount;
  final double actualAmount;
  final double variance;
  final double variancePercent;

  const BudgetVsActualItem({
    required this.budgetId, required this.year, required this.month,
    required this.accountId, required this.accountCode, required this.accountName,
    this.costCenterId, this.costCenterName,
    required this.budgetedAmount, required this.actualAmount,
    required this.variance, required this.variancePercent,
  });

  factory BudgetVsActualItem.fromJson(Map<String, dynamic> j) => BudgetVsActualItem(
    budgetId: j['budgetId'] as String? ?? '',
    year: j['year'] as int? ?? 0,
    month: j['month'] as int? ?? 0,
    accountId: j['accountId'] as String? ?? '',
    accountCode: j['accountCode'] as String? ?? '',
    accountName: j['accountName'] as String? ?? '',
    costCenterId: j['costCenterId'] as String?,
    costCenterName: j['costCenterName'] as String?,
    budgetedAmount: (j['budgetedAmount'] as num?)?.toDouble() ?? 0,
    actualAmount: (j['actualAmount'] as num?)?.toDouble() ?? 0,
    variance: (j['variance'] as num?)?.toDouble() ?? 0,
    variancePercent: (j['variancePercent'] as num?)?.toDouble() ?? 0,
  );
}

final class BudgetState {
  final List<BudgetItem> items;
  final List<BudgetVsActualItem> reportItems;
  final bool loading;
  final String? error;
  const BudgetState({this.items = const [], this.reportItems = const [], this.loading = false, this.error});
  BudgetState copyWith({List<BudgetItem>? items, List<BudgetVsActualItem>? reportItems, bool? loading, String? error}) =>
    BudgetState(items: items ?? this.items, reportItems: reportItems ?? this.reportItems, loading: loading ?? this.loading, error: error);
}

final class BudgetNotifier extends Notifier<BudgetState> {
  @override
  BudgetState build() => const BudgetState();

  Future<void> load({int? year, int? month}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      var uri = 'budgets';
      if (year != null && month != null) uri += '?year=$year&month=$month';
      final r = await dio.get(uri);
      final data = r.data as List;
      state = BudgetState(items: data.map((e) => BudgetItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar presupuestos', loading: false);
    }
  }

  Future<void> loadReport(int year, int month) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('financial-reports/budget-vs-actual?year=$year&month=$month');
      final data = r.data['items'] as List;
      state = BudgetState(reportItems: data.map((e) => BudgetVsActualItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar reporte', loading: false);
    }
  }
}

final budgetProvider = NotifierProvider<BudgetNotifier, BudgetState>(BudgetNotifier.new);

final class AccountOption {
  final String id;
  final String code;
  final String name;
  final String type;
  const AccountOption({required this.id, required this.code, required this.name, required this.type});
}

final class AccountListNotifier extends Notifier<List<AccountOption>> {
  @override
  List<AccountOption> build() => [];

  Future<void> load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('accounts');
      final data = r.data as List;
      state = data.map((e) => AccountOption(
        id: e['id'] as String? ?? '',
        code: e['code'] as String? ?? '',
        name: e['name'] as String? ?? '',
        type: e['type'] as String? ?? '',
      )).toList();
    } catch (_) {
      state = [];
    }
  }
}

final accountListProvider = NotifierProvider<AccountListNotifier, List<AccountOption>>(AccountListNotifier.new);
