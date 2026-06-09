import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/features/accounting/providers/accounting_provider.dart';
import 'package:zorvian/shared/ds/ds.dart';
import 'package:zorvian/auth/auth_provider.dart';

final class IncomeStatementPage extends ConsumerStatefulWidget {
  const IncomeStatementPage({super.key});
  @override
  ConsumerState<IncomeStatementPage> createState() => _IncomeStatementPageState();
}

final class _IncomeStatementPageState extends ConsumerState<IncomeStatementPage> {
  String? _selectedPeriodId;
  IncomeStatementData? _data;
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => ref.read(accountingProvider.notifier).loadPeriods());
  }

  Future<void> _loadIncomeStatement() async {
    if (_selectedPeriodId == null) return;
    setState(() { _loading = true; _error = null; _data = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('financial-reports/income-statement/$_selectedPeriodId');
      setState(() {
        _data = IncomeStatementData.fromJson(r.data as Map<String, dynamic>);
        _loading = false;
      });
    } catch (e) {
      setState(() { _error = e.toString(); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final state = ref.watch(accountingProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Estado de Resultados')),
      body: Padding(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: ZSelect<String>(
                    value: _selectedPeriodId,
                    label: 'Período',
                    hint: 'Seleccione un período',
                    items: state.periods
                        .map((p) => DropdownMenuItem(value: p.id, child: Text(p.name)))
                        .toList(),
                    onChanged: (v) => setState(() => _selectedPeriodId = v),
                  ),
                ),
                const SizedBox(width: ZSpacing.sm),
                if (_selectedPeriodId != null)
                  ZButton(
                    text: 'Consultar',
                    onPressed: () => _loadIncomeStatement(),
                    icon: Icons.search,
                  ),
              ],
            ),
            const SizedBox(height: ZSpacing.lg),
            if (_loading)
              const Expanded(child: Center(child: ZInlineLoading(message: 'Cargando...'))),
            if (_error != null)
              Expanded(child: ZErrorDisplay(message: _error!, onRetry: () => _loadIncomeStatement())),
            if (_data != null && !_loading)
              Expanded(
                child: ListView(
                  children: [
                    Text(_data!.periodName, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                    const SizedBox(height: ZSpacing.lg),
                    ZCard(
                      child: Padding(
                        padding: const EdgeInsets.all(ZSpacing.md),
                        child: Column(
                          children: [
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
                      ),
                    ),
                  ],
                ),
              ),
            if (_data == null && !_loading && _error == null)
              const Expanded(child: Center(child: ZEmptyState(icon: Icons.assessment, title: 'Seleccione un período y presione Consultar'))),
          ],
        ),
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
