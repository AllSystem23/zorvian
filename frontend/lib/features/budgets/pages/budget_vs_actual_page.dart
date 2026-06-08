import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/budget_provider.dart';

final class BudgetVsActualPage extends ConsumerStatefulWidget {
  const BudgetVsActualPage({super.key});
  @override
  ConsumerState<BudgetVsActualPage> createState() => _BudgetVsActualPageState();
}

final class _BudgetVsActualPageState extends ConsumerState<BudgetVsActualPage> {
  int _year = DateTime.now().year;
  int _month = DateTime.now().month;

  @override
  void initState() {
    super.initState();
    _loadReport();
  }

  Future<void> _loadReport() async => ref.read(budgetProvider.notifier).loadReport(_year, _month);

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(budgetProvider);
    final theme = Theme.of(context);
    final items = state.reportItems;

    var totalBudgeted = items.fold(0.0, (s, i) => s + i.budgetedAmount);
    var totalActual = items.fold(0.0, (s, i) => s + i.actualAmount);
    var totalVariance = totalActual - totalBudgeted;

    return Scaffold(
      appBar: AppBar(title: const Text('Presupuesto vs Real')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    decoration: const InputDecoration(labelText: 'Año', prefixIcon: Icon(Icons.calendar_today)),
                    keyboardType: TextInputType.number,
                    controller: TextEditingController(text: _year.toString()),
                    onChanged: (v) => _year = int.tryParse(v) ?? _year,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextField(
                    decoration: const InputDecoration(labelText: 'Mes', prefixIcon: Icon(Icons.date_range)),
                    keyboardType: TextInputType.number,
                    controller: TextEditingController(text: _month.toString()),
                    onChanged: (v) => _month = int.tryParse(v) ?? _month,
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(icon: const Icon(Icons.search), onPressed: _loadReport),
              ],
            ),
          ),
          if (items.isNotEmpty)
            ZCard(
              margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
              padding: const EdgeInsets.all(12),
              child: Column(
                  children: [
                    _summaryRow('Presupuestado', totalBudgeted, theme.colorScheme.primary),
                    const SizedBox(height: 4),
                    _summaryRow('Real', totalActual, theme.colorScheme.tertiary),
                    const SizedBox(height: 4),
                    _summaryRow('Variación', totalVariance, totalVariance >= 0 ? Colors.green : Colors.red),
                  ],
                ),
              ),
          Expanded(
            child: state.loading
                ? const Center(child: CircularProgressIndicator())
                : state.error != null
                    ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
                    : items.isEmpty
                        ? const Center(child: Text('Sin datos para el período seleccionado'))
                        : RefreshIndicator(
                            onRefresh: _loadReport,
                            child: ListView.separated(
                              itemCount: items.length,
                              separatorBuilder: (_, _) => const Divider(height: 1),
                              itemBuilder: (_, i) {
                                final b = items[i];
                                final variColor = b.variance >= 0 ? Colors.green : Colors.red;
                                return ListTile(
                                  title: Text('${b.accountCode} - ${b.accountName}', style: const TextStyle(fontWeight: FontWeight.w600)),
                                  subtitle: Text(b.costCenterName != null ? 'Centro: ${b.costCenterName}' : ''),
                                  trailing: Column(
                                    crossAxisAlignment: CrossAxisAlignment.end,
                                    mainAxisAlignment: MainAxisAlignment.center,
                                    children: [
                                      Text('\$${b.budgetedAmount.toStringAsFixed(2)} / \$${b.actualAmount.toStringAsFixed(2)}', style: const TextStyle(fontSize: 12)),
                                      Text('\$${b.variance.toStringAsFixed(2)} (${b.variancePercent.toStringAsFixed(1)}%)', style: TextStyle(color: variColor, fontWeight: FontWeight.w600, fontSize: 13)),
                                    ],
                                  ),
                                );
                              },
                            ),
                          ),
          ),
        ],
      ),
    );
  }

  Widget _summaryRow(String label, double amount, Color color) => Row(
    mainAxisAlignment: MainAxisAlignment.spaceBetween,
    children: [
      Text(label, style: const TextStyle(fontWeight: FontWeight.w600)),
      Text('\$${amount.toStringAsFixed(2)}', style: TextStyle(color: color, fontWeight: FontWeight.bold, fontSize: 16)),
    ],
  );
}
