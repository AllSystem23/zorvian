import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/accounting_provider.dart';

final class IncomeStatementPage extends ConsumerStatefulWidget {
  const IncomeStatementPage({super.key});
  @override
  ConsumerState<IncomeStatementPage> createState() => _IncomeStatementPageState();
}

final class _IncomeStatementPageState extends ConsumerState<IncomeStatementPage> {
  IncomeStatementData? _data;
  final bool _loading = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Estado de Resultados')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _data == null
              ? const Center(child: Text('Seleccione un período contable'))
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    Text(_data!.periodName, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 24),
                    _row('Ingresos', _data!.totalIncome, Colors.teal),
                    const Divider(),
                    _row('Costo de Ventas', _data!.totalCost, Colors.red),
                    const Divider(),
                    _row('Utilidad Bruta', _data!.grossProfit, _data!.grossProfit >= 0 ? Colors.green : Colors.red, bold: true),
                    const Divider(),
                    _row('Gastos Operativos', _data!.totalExpenses, Colors.purple),
                    const Divider(thickness: 2),
                    _row('Utilidad Neta', _data!.netIncome, _data!.netIncome >= 0 ? Colors.green : Colors.red, bold: true, large: true),
                  ],
                ),
    );
  }

  Widget _row(String label, double amount, Color color, {bool bold = false, bool large = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal, fontSize: large ? 16 : 14)),
          Text('\$${amount.toStringAsFixed(2)}',
            style: TextStyle(
              fontWeight: bold ? FontWeight.bold : FontWeight.w600,
              fontSize: large ? 16 : 14,
              color: color,
            )),
        ],
      ),
    );
  }
}
