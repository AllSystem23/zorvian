import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fl_chart/fl_chart.dart';
import '../../../features/bi/providers/bi_provider.dart';
import '../../../../shared/ds/ds.dart';
import 'bi_kpi_card.dart';
import 'bi_line_chart.dart';

class SalesPredictionSection extends ConsumerWidget {
  const SalesPredictionSection({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final monthAsync = ref.watch(biSalesPredictionNextMonthProvider);
    final weekAsync = ref.watch(biSalesPredictionNextWeekProvider);
    final projAsync = ref.watch(biMonthlyProjectionProvider);
    final palette = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Pronóstico de Ventas', style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        projAsync.when(
          loading: () => const SizedBox(height: 120, child: Center(child: CircularProgressIndicator())),
          error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
          data: (proj) => LayoutBuilder(
            builder: (_, constraints) => Wrap(
              spacing: 12, runSpacing: 12,
              children: [
                SizedBox(
                  width: (constraints.maxWidth - 12) / 2,
                  child: BiKpiCard(
                    label: 'Proyectado al Mes',
                    value: '\$${proj.projectedTotal.toStringAsFixed(0)}',
                    icon: Icons.trending_up,
                    color: palette.primary,
                    changePercent: proj.salesSoFar > 0
                      ? ((proj.projectedTotal - proj.salesSoFar) / proj.salesSoFar * 100)
                      : null,
                  ),
                ),
                SizedBox(
                  width: (constraints.maxWidth - 12) / 2,
                  child: BiKpiCard(
                    label: 'Ventas Acumuladas',
                    value: '\$${proj.salesSoFar.toStringAsFixed(0)}',
                    icon: Icons.account_balance,
                    color: palette.tertiary,
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        monthAsync.when(
          loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
          error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
          data: (month) {
            final predicted = month.predictions;
            if (predicted.isEmpty) return const SizedBox.shrink();
            final spots = predicted.asMap().entries.map((e) => FlSpot(e.key.toDouble(), e.value.predictedSales)).toList();
            final upperSpots = predicted.asMap().entries.map((e) => FlSpot(e.key.toDouble(), e.value.upperBound)).toList();
            final lowerSpots = predicted.asMap().entries.map((e) => FlSpot(e.key.toDouble(), e.value.lowerBound)).toList();
            final primary = palette.primary;

            return Column(
              children: [
                SizedBox(
                  height: 200,
                  child: BiLineChart(
                    series: [
                      LineChartSeries(spots, color: primary, showArea: true),
                      LineChartSeries(upperSpots, color: primary.withAlpha(80)),
                      LineChartSeries(lowerSpots, color: primary.withAlpha(80)),
                    ],
                  ),
                ),
                const SizedBox(height: 8),
                Text('Pronóstico mensual: \$${month.totalPredicted.toStringAsFixed(0)}  •  Promedio diario: \$${month.dailyAverage.toStringAsFixed(0)}',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
              ],
            );
          },
        ),
        const SizedBox(height: 8),
        weekAsync.when(
          loading: () => const SizedBox(height: 120, child: Center(child: CircularProgressIndicator())),
          error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
          data: (days) {
            if (days.isEmpty) return const SizedBox.shrink();
            return ZCard(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Próximos 7 días', style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 8),
                  ...days.map((d) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        SizedBox(width: 100, child: Text(d.dayOfWeek, style: Theme.of(context).textTheme.bodySmall)),
                        Text('\$${d.predictedSales.toStringAsFixed(0)}', style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
                        const Spacer(),
                        Text('\$${d.lowerBound.toStringAsFixed(0)} – \$${d.upperBound.toStringAsFixed(0)}',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
                      ],
                    ),
                  )),
                ],
              ),
            );
          },
        ),
      ],
    );
  }
}
