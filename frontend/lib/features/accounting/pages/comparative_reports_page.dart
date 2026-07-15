import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/features/accounting/providers/accounting_provider.dart';
import 'package:zorvian/features/accounting/providers/enhanced_reports_provider.dart';
import 'package:zorvian/shared/ds/ds.dart';
import 'package:zorvian/core/widgets/bi/bi_bar_chart.dart';
import 'package:zorvian/features/accounting/models/enhanced_report_models.dart';

extension ComparativeLineX on ComparativeLine {
  BarChartItem toBarChartItem(ThemeData theme) {
    final current = periods.length > 1 ? periods[1].amount : periods[0].amount;
    return BarChartItem(concept, current, color: theme.colorScheme.primary);
  }
}

final class ComparativeReportsPage extends ConsumerStatefulWidget {
  const ComparativeReportsPage({super.key});
  @override
  ConsumerState<ComparativeReportsPage> createState() => _ComparativeReportsPageState();
}

final class _ComparativeReportsPageState extends ConsumerState<ComparativeReportsPage> {
  String? _periodId1;
  String? _periodId2;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => ref.read(accountingProvider.notifier).loadPeriods());
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final state = ref.watch(accountingProvider);
    final canCompare = _periodId1 != null && _periodId2 != null && _periodId1 != _periodId2;
    final periods = state.periods
        .map((p) => DropdownMenuItem(value: p.id, child: Text(p.name)))
        .toList();

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Comparar dos períodos', style: theme.textTheme.titleMedium),
            const SizedBox(height: ZSpacing.md),
            ZSelect<String>(
              value: _periodId1,
              label: 'Período 1',
              hint: 'Seleccione el primer período',
              items: periods,
              onChanged: (v) => setState(() => _periodId1 = v),
            ),
            const SizedBox(height: ZSpacing.sm),
            ZSelect<String>(
              value: _periodId2,
              label: 'Período 2',
              hint: 'Seleccione el segundo período',
              items: periods.where((p) => p.value != _periodId1).toList(),
              onChanged: (v) => setState(() => _periodId2 = v),
            ),
            const SizedBox(height: ZSpacing.lg),
            if (canCompare)
              ZButton(
                text: 'Comparar',
                onPressed: () => setState(() {}),
                icon: Icons.compare_arrows,
              ),
            const SizedBox(height: ZSpacing.lg),
            if (canCompare)
              ref.watch(enhancedComparativeProvider({
                'reportType': 'income_statement',
                'periodIds': [_periodId1!, _periodId2!],
              })).when(
                loading: () => const Center(child: ZInlineLoading(message: 'Cargando...')),
                error: (e, _) => ZErrorDisplay(message: e.toString(), onRetry: () => setState(() {})),
                data: (data) => Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    ZStatCard(title: 'Tipo Reporte', value: data.reportType, variant: ZStatVariant.info),
                    ZStatCard(title: 'Total Período Actual', value: '\$${data.totalCurrent.toStringAsFixed(2)}', variant: ZStatVariant.primary),
                    ZStatCard(title: 'Total Período Anterior', value: '\$${data.totalPrevious.toStringAsFixed(2)}', variant: ZStatVariant.primary),
                    ZStatCard(title: 'Varianza', value: '\$${data.totalVariance.toStringAsFixed(2)}',
                        variant: data.totalVariance >= 0 ? ZStatVariant.success : ZStatVariant.danger),
                    ZStatCard(title: 'Varianza %', value: '${data.totalVariancePercent.toStringAsFixed(2)}%', variant: ZStatVariant.info),
                    
                    const SizedBox(height: ZSpacing.lg),
                    Text('Gráfico Comparativo', style: theme.textTheme.titleSmall),
                    const SizedBox(height: ZSpacing.sm),
                    ZCard(
                      child: SizedBox(
                        height: 300,
                        child: BiBarChart(
                          items: data.lines.take(10).map((l) => l.toBarChartItem(theme)).toList(),
                        ),
                      ),
                    ),
                    
                    const SizedBox(height: ZSpacing.lg),
                    Text('Comparativo por Línea', style: theme.textTheme.titleSmall),
                    const SizedBox(height: ZSpacing.sm),
                    ZDataTable<dynamic>(
                      columns: const [
                        ZColumn(id: 'concept', label: 'Concepto'),
                        ZColumn(id: 'current', label: 'Actual', numeric: true),
                        ZColumn(id: 'previous', label: 'Anterior', numeric: true),
                        ZColumn(id: 'variance', label: 'Varianza', numeric: true),
                        ZColumn(id: 'pct', label: '%', numeric: true),
                      ],
                      rows: data.lines,
                      rowMapper: (l) {
                        final current = l.periods.length > 1 ? l.periods[1].amount : l.periods[0].amount;
                        final previous = l.periods[0].amount;
                        return DataRow(cells: [
                          DataCell(Text(l.concept, style: const TextStyle(fontSize: 11))),
                          DataCell(Text('\$${current.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11))),
                          DataCell(Text('\$${previous.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11))),
                          DataCell(Text('\$${l.variance.toStringAsFixed(2)}', style: TextStyle(fontSize: 11, color: l.variance >= 0 ? Colors.green : Colors.red))),
                          DataCell(Text('${l.variancePercent.toStringAsFixed(1)}%', style: TextStyle(fontSize: 11, color: l.variancePercent >= 0 ? Colors.green : Colors.red))),
                        ]);
                      },
                    ),
                  ],
                ),
              ),
            if (!canCompare)
              ZEmptyState(icon: Icons.compare_arrows, title: 'Seleccione dos períodos diferentes para comparar'),
          ],
        ),
      ),
    );
  }
}
