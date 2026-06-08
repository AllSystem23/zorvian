import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:nexora/auth/auth_provider.dart';
import 'package:nexora/features/bi/models/bi_models.dart';

class BiState {
  final BiExecutive? executive;
  final bool loading;
  final String? error;
  const BiState({this.executive, this.loading = false, this.error});
  BiState copyWith({BiExecutive? executive, bool? loading, String? error}) =>
      BiState(executive: executive ?? this.executive, loading: loading ?? this.loading, error: error);
}

class BiNotifier extends Notifier<BiState> {
  @override
  BiState build() => const BiState();

  Future<void> loadExecutive() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final res = await dio.get('bi/executive-summary');
      state = state.copyWith(executive: BiExecutive.fromJson(res.data), loading: false);
    } catch (e) {
      state = state.copyWith(error: e.toString(), loading: false);
    }
  }
}

final biProvider = NotifierProvider<BiNotifier, BiState>(BiNotifier.new);

final biSalesTrendProvider = FutureProvider.autoDispose.family<List<BiMonthSales>, int>((ref, months) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('bi/sales-trend', params: {'months': months});
  final data = res.data['monthly'] as List;
  return data.map((e) => BiMonthSales.fromJson(e)).toList();
});

final biArAgingProvider = FutureProvider.autoDispose((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('bi/ar-aging');
  return BiArAging.fromJson(res.data);
});

final biApAgingProvider = FutureProvider.autoDispose((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('bi/ap-aging');
  return BiApAging.fromJson(res.data);
});

final biFinancialRatiosProvider = FutureProvider.autoDispose((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('bi/financial-ratios');
  return BiFinancialRatios.fromJson(res.data);
});

final biComparativeIncomeProvider = FutureProvider.autoDispose((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('bi/comparative-income');
  return BiComparativeIncome.fromJson(res.data);
});

final biCashFlowProvider = FutureProvider.autoDispose((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('bi/cash-flow');
  return BiCashFlow.fromJson(res.data);
});

final biInventorySummaryProvider = FutureProvider.autoDispose((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('bi/inventory-summary');
  return BiInventorySummary.fromJson(res.data);
});

final biPayrollSummaryProvider = FutureProvider.autoDispose((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('bi/payroll-summary');
  return BiPayrollSummary.fromJson(res.data);
});

final biSalesPredictionNextWeekProvider = FutureProvider.autoDispose<List<BiSalesPredictionDaily>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('sales/predictions/next-week');
  final data = res.data as List;
  return data.map((e) => BiSalesPredictionDaily.fromJson(e)).toList();
});

final biSalesPredictionNextMonthProvider = FutureProvider.autoDispose<BiMonthlySalesPrediction>((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('sales/predictions/next-month');
  return BiMonthlySalesPrediction.fromJson(res.data);
});

final biMonthlyProjectionProvider = FutureProvider.autoDispose<BiMonthlyProjection>((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('sales/predictions/monthly-total');
  return BiMonthlyProjection.fromJson(res.data);
});

final purchaseRecommendationProvider = FutureProvider.autoDispose<PurchaseRecommendationSummary>((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('purchases/recommendations', params: {'demandDays': 30, 'leadTimeDays': 7});
  return PurchaseRecommendationSummary.fromJson(res.data);
});

final expenseClassificationProvider = FutureProvider.autoDispose.family<ExpenseClassificationResponse, ({String description, double amount})>((ref, params) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('expense-classification/predict', params: {'description': params.description, 'amount': params.amount});
  return ExpenseClassificationResponse.fromJson(res.data);
});

final accountingAnomaliesProvider = FutureProvider.autoDispose<AccountingAnomalyReport>((ref) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('accounting-assistant/anomalies', params: {'daysBack': 30});
  return AccountingAnomalyReport.fromJson(res.data);
});
