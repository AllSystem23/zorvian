import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class PredictionsState {
  final Map<String, dynamic>? salesPredictions;
  final List<dynamic> expenseClassifications;
  final List<dynamic> purchaseRecommendations;
  final List<dynamic> absenteeismPredictions;
  final bool loading;

  PredictionsState({
    this.salesPredictions,
    this.expenseClassifications = const [],
    this.purchaseRecommendations = const [],
    this.absenteeismPredictions = const [],
    this.loading = false,
  });

  PredictionsState copyWith({
    Map<String, dynamic>? salesPredictions,
    List<dynamic>? expenseClassifications,
    List<dynamic>? purchaseRecommendations,
    List<dynamic>? absenteeismPredictions,
    bool? loading,
  }) => PredictionsState(
    salesPredictions: salesPredictions ?? this.salesPredictions,
    expenseClassifications: expenseClassifications ?? this.expenseClassifications,
    purchaseRecommendations: purchaseRecommendations ?? this.purchaseRecommendations,
    absenteeismPredictions: absenteeismPredictions ?? this.absenteeismPredictions,
    loading: loading ?? this.loading,
  );
}

class PredictionsNotifier extends Notifier<PredictionsState> {
  @override
  PredictionsState build() => PredictionsState();

  Future<void> loadSalesPredictions() async {
    state = state.copyWith(loading: true);
    try {
      final dio = ref.read(dioClientProvider);
      final res = await dio.get('sales/predictions/next-month');
      state = state.copyWith(salesPredictions: res.data as Map<String, dynamic>?, loading: false);
    } catch (e) {
      state = state.copyWith(loading: false);
    }
  }

  Future<void> loadExpenseClassifications() async {
    try {
      final dio = ref.read(dioClientProvider);
      final res = await dio.get('expense-classification/predict');
      state = state.copyWith(expenseClassifications: res.data as List? ?? []);
    } catch (_) {}
  }

  Future<void> loadPurchaseRecommendations() async {
    try {
      final dio = ref.read(dioClientProvider);
      final res = await dio.get('purchases/recommendations');
      state = state.copyWith(purchaseRecommendations: res.data as List? ?? []);
    } catch (_) {}
  }
}

final predictionsProvider = NotifierProvider<PredictionsNotifier, PredictionsState>(PredictionsNotifier.new);
