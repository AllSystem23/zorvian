import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/features/accounting/models/enhanced_report_models.dart';

final enhancedEquityChangesProvider = FutureProvider.autoDispose.family<EquityChangeResponse, String>((ref, periodId) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('financial-reports/equity-changes/$periodId');
  return EquityChangeResponse.fromJson(res.data);
});

final enhancedCashFlowProvider = FutureProvider.autoDispose.family<CashFlowStatementResponse, String>((ref, periodId) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('financial-reports/cash-flow/$periodId');
  return CashFlowStatementResponse.fromJson(res.data);
});

final enhancedComparativeProvider = FutureProvider.autoDispose.family<ComparativeReportResponse, Map<String, dynamic>>((ref, params) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.post('financial-reports/comparative', data: params);
  return ComparativeReportResponse.fromJson(res.data);
});

final enhancedAssistantProvider = FutureProvider.autoDispose.family<FinancialAssistantResponse, String>((ref, query) async {
  final dio = ref.read(dioClientProvider);
  final res = await dio.post('financial-reports/assistant/ask', data: {'query': query});
  return FinancialAssistantResponse.fromJson(res.data);
});
