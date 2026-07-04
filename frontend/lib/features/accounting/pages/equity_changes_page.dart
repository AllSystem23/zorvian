import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/features/accounting/providers/accounting_provider.dart';
import 'package:zorvian/features/accounting/providers/enhanced_reports_provider.dart';
import 'package:zorvian/shared/ds/ds.dart';

final class EquityChangesPage extends ConsumerStatefulWidget {
  const EquityChangesPage({super.key});
  @override
  ConsumerState<EquityChangesPage> createState() => _EquityChangesPageState();
}

final class _EquityChangesPageState extends ConsumerState<EquityChangesPage> {
  String? _selectedPeriodId;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => ref.read(accountingProvider.notifier).loadPeriods());
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final state = ref.watch(accountingProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Cambios en el Patrimonio')),
      body: Padding(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ZSelect<String>(
              value: _selectedPeriodId,
              label: 'Período',
              hint: 'Seleccione un período',
              items: state.periods
                  .map((p) => DropdownMenuItem(value: p.id, child: Text(p.name)))
                  .toList(),
              onChanged: (v) => setState(() => _selectedPeriodId = v),
            ),
            const SizedBox(height: ZSpacing.lg),
            if (_selectedPeriodId != null)
              ref.watch(enhancedEquityChangesProvider(_selectedPeriodId!)).when(
                loading: () => const Center(child: ZInlineLoading(message: 'Cargando...')),
                error: (e, _) => ZErrorDisplay(message: e.toString(), onRetry: () => setState(() {})),
                data: (data) => Expanded(
                  child: ListView(
                    children: [
                      Text(data.periodName, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                      const SizedBox(height: ZSpacing.sm),
                      ZStatCard(title: 'Patrimonio Inicial', value: '\$${data.totalOpeningEquity.toStringAsFixed(2)}', variant: ZStatVariant.info),
                      ZStatCard(title: 'Aumentos', value: '\$${data.totalAdditions.toStringAsFixed(2)}', variant: ZStatVariant.success),
                      ZStatCard(title: 'Disminuciones', value: '\$${data.totalDeductions.toStringAsFixed(2)}', variant: ZStatVariant.danger),
                      ZStatCard(title: 'Patrimonio Final', value: '\$${data.totalEndingEquity.toStringAsFixed(2)}', variant: ZStatVariant.primary),
                      const SizedBox(height: ZSpacing.lg),
                      Text('Detalle por Concepto', style: theme.textTheme.titleSmall),
                      const SizedBox(height: ZSpacing.sm),
                      ZDataTable<dynamic>(
                        columns: const [
                          ZColumn(id: 'concept', label: 'Concepto'),
                          ZColumn(id: 'opening', label: 'Inicial', numeric: true),
                          ZColumn(id: 'additions', label: 'Aumentos', numeric: true),
                          ZColumn(id: 'deductions', label: 'Disminuciones', numeric: true),
                          ZColumn(id: 'ending', label: 'Final', numeric: true),
                        ],
                        rows: data.items,
                        rowMapper: (i) => DataRow(cells: [
                          DataCell(Text(i.concept, style: const TextStyle(fontSize: 11))),
                          DataCell(Text('\$${i.openingBalance.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11))),
                          DataCell(Text('\$${i.additions.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11, color: Colors.green))),
                          DataCell(Text('\$${i.deductions.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11, color: Colors.red))),
                          DataCell(Text('\$${i.endingBalance.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11, fontWeight: FontWeight.bold))),
                        ]),
                      ),
                    ],
                  ),
                ),
              ),
            if (_selectedPeriodId == null)
              const Expanded(child: Center(child: ZEmptyState(icon: Icons.account_balance, title: 'Seleccione un período contable'))),
          ],
        ),
      ),
    );
  }
}
