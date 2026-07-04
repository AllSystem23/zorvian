import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/features/accounting/providers/accounting_provider.dart';
import 'package:zorvian/shared/ds/ds.dart';
import 'package:zorvian/auth/auth_provider.dart';

final class TrialBalancePage extends ConsumerStatefulWidget {
  const TrialBalancePage({super.key});
  @override
  ConsumerState<TrialBalancePage> createState() => _TrialBalancePageState();
}

final class _TrialBalancePageState extends ConsumerState<TrialBalancePage> {
  String? _selectedPeriodId;
  TrialBalanceData? _data;
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => ref.read(accountingProvider.notifier).loadPeriods());
  }

  Future<void> _loadTrialBalance() async {
    if (_selectedPeriodId == null) return;
    setState(() { _loading = true; _error = null; _data = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('financial-reports/trial-balance/$_selectedPeriodId');
      setState(() {
        _data = TrialBalanceData.fromJson(r.data as Map<String, dynamic>);
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
      appBar: AppBar(title: const Text('Balanza de Comprobación')),
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
                    onPressed: () => _loadTrialBalance(),
                    icon: Icons.search,
                  ),
              ],
            ),
            const SizedBox(height: ZSpacing.lg),
            if (_loading)
              const Expanded(child: Center(child: ZInlineLoading(message: 'Cargando...'))),
            if (_error != null)
              Expanded(child: ZErrorDisplay(message: _error!, onRetry: () => _loadTrialBalance())),
            if (_data != null && !_loading)
              Expanded(
                child: ListView(
                  children: [
                    Text(_data!.periodName, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                    const SizedBox(height: ZSpacing.sm),
                    ZDataTable<dynamic>(
                      columns: const [
                        ZColumn(id: 'code', label: 'Código'),
                        ZColumn(id: 'account', label: 'Cuenta'),
                        ZColumn(id: 'opening', label: 'Saldo Inicial', numeric: true),
                        ZColumn(id: 'debits', label: 'Débitos', numeric: true),
                        ZColumn(id: 'credits', label: 'Créditos', numeric: true),
                        ZColumn(id: 'ending', label: 'Saldo Final', numeric: true),
                      ],
                      rows: _data!.items,
                      rowMapper: (i) => DataRow(cells: [
                        DataCell(Text(i.accountCode, style: const TextStyle(fontSize: 11))),
                        DataCell(Text(i.accountName, style: const TextStyle(fontSize: 11))),
                        DataCell(Text('\$${i.openingBalance.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11))),
                        DataCell(Text('\$${i.debitMovements.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11))),
                        DataCell(Text('\$${i.creditMovements.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11))),
                        DataCell(Text('\$${i.endingBalance.toStringAsFixed(2)}', style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: i.endingBalance >= 0 ? Colors.green : Colors.red))),
                      ]),
                    ),
                  ],
                ),
              ),
            if (_data == null && !_loading && _error == null)
              const Expanded(child: Center(child: ZEmptyState(icon: Icons.account_balance, title: 'Seleccione un período y presione Consultar'))),
          ],
        ),
      ),
    );
  }
}
