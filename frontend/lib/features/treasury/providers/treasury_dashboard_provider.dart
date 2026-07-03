import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class TreasuryDashboardSummary {
  final double totalBankBalance;
  final int pendingDeposits;
  final int outstandingChecks;
  final int pendingReconciliations;

  const TreasuryDashboardSummary({
    required this.totalBankBalance,
    required this.pendingDeposits,
    required this.outstandingChecks,
    required this.pendingReconciliations,
  });

  factory TreasuryDashboardSummary.fromJson(Map<String, dynamic> j) => TreasuryDashboardSummary(
    totalBankBalance: (j['totalBankBalance'] as num?)?.toDouble() ?? 0,
    pendingDeposits: j['pendingDeposits'] as int? ?? 0,
    outstandingChecks: j['outstandingChecks'] as int? ?? 0,
    pendingReconciliations: j['pendingReconciliations'] as int? ?? 0,
  );
}

final treasuryDashboardSummaryProvider = FutureProvider<TreasuryDashboardSummary>((ref) async {
  final dio = ref.read(dioClientProvider);
  final r = await dio.get('treasury/dashboard-summary');
  return TreasuryDashboardSummary.fromJson(r.data);
});
