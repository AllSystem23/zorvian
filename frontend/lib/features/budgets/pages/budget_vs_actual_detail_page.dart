import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/budget_detail_provider.dart';

final class BudgetVsActualDetailPage extends ConsumerStatefulWidget {
  final int year;
  final int month;
  const BudgetVsActualDetailPage({super.key, required this.year, required this.month});
  @override
  ConsumerState<BudgetVsActualDetailPage> createState() => _BudgetVsActualDetailPageState();
}

final class _BudgetVsActualDetailPageState extends ConsumerState<BudgetVsActualDetailPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      ref.read(budgetDetailProvider.notifier).loadByPeriod(widget.year, widget.month);
      ref.read(budgetDetailProvider.notifier).loadTracking(year: widget.year, month: widget.month);
    });
  }

  Color _varianceColor(double variancePercent) {
    if (variancePercent > 10) return Colors.red;
    if (variancePercent < -10) return Colors.green;
    if (variancePercent > 5) return Colors.orange;
    return Colors.grey;
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(budgetDetailProvider);
    final theme = Theme.of(context);

    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: ZCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: ZStatCard(
                          title: 'Presupuestado',
                          value: '\$${state.details.fold<double>(0, (s, d) => s + d.budgetedAmount.toDouble()).toStringAsFixed(2)}',
                          icon: Icons.account_balance_wallet,
                          variant: ZStatVariant.info,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: ZStatCard(
                          title: 'Real',
                          value: '\$${state.trackingItems.fold<double>(0, (s, t) => s + t.actualAmount.toDouble()).toStringAsFixed(2)}',
                          icon: Icons.trending_up,
                          variant: ZStatVariant.success,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: ZStatCard(
                          title: 'Variación',
                          value: '\$${(state.trackingItems.fold<double>(0, (s, t) => s + t.variance)).toStringAsFixed(2)}',
                          icon: Icons.compare_arrows,
                          variant: ZStatVariant.warning,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          Expanded(
            child: state.loading
                ? const Center(child: CircularProgressIndicator())
                : state.error != null
                    ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
                    : ListView.builder(
                        itemCount: state.details.length,
                        itemBuilder: (_, i) {
                          final d = state.details[i];
                          final tracking = state.trackingItems.where((t) => t.budgetDetailId == d.id).toList();
                          final actualAmount = tracking.fold<double>(0, (s, t) => s + t.actualAmount);
                          final variance = d.budgetedAmount - actualAmount;
                          final variancePercent = d.budgetedAmount > 0 ? (variance / d.budgetedAmount * 100).toDouble() : 0.0;

                          return Card(
                            margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                            child: ExpansionTile(
                              leading: Icon(
                                variancePercent > 5 ? Icons.warning : Icons.check_circle,
                                color: _varianceColor(variancePercent),
                              ),
                              title: Text('${d.accountCode} - ${d.accountName}',
                                style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
                              ),
                              subtitle: Text(
                                'Presupuestado: \$${d.budgetedAmount.toDouble().toStringAsFixed(2)} | Real: \$${actualAmount.toStringAsFixed(2)}',
                                style: const TextStyle(fontSize: 12),
                              ),
                              trailing: Text(
                                '${variancePercent.toStringAsFixed(1)}%',
                                style: TextStyle(
                                  color: _varianceColor(variancePercent),
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              children: tracking.map((t) => ListTile(
                                dense: true,
                                title: Text('Referencia: ${t.sourceReference ?? "N/A"}'),
                                subtitle: Text('\$${t.actualAmount.toStringAsFixed(2)} - ${t.notes ?? ""}'),
                                trailing: Text('${t.month}/${t.year}'),
                              )).toList(),
                            ),
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }
}
