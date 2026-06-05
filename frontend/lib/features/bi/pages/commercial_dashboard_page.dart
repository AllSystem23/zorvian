import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fl_chart/fl_chart.dart';
import '../../../core/widgets/bi/bi_line_chart.dart';
import '../../../core/widgets/bi/bi_bar_chart.dart';
import '../../../core/widgets/bi/bi_kpi_card.dart';
import '../providers/bi_provider.dart';

class CommercialDashboardPage extends ConsumerWidget {
  const CommercialDashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final salesTrendAsync = ref.watch(biSalesTrendProvider(12));

    return Scaffold(
      appBar: AppBar(title: const Text('Panel Comercial')),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(biSalesTrendProvider(12)),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text('Tendencia de Ventas (12 meses)', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            salesTrendAsync.when(
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => Card(child: Padding(padding: const EdgeInsets.all(16), child: Text('Error: $e'))),
              data: (months) {
                final totals = months.map((m) => m.total).toList();
                final counts = months.map((m) => m.count.toDouble()).toList();
                final labels = months.map((m) {
                  const monthNames = ['', 'Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
                  return monthNames[m.month];
                }).toList();
                final primary = Theme.of(context).colorScheme.primary;
                final tertiary = Theme.of(context).colorScheme.tertiary;

                return Column(
                  children: [
                    SizedBox(
                      height: 200,
                      child: BiLineChart(
                        series: [
                          LineChartSeries(
                            totals.asMap().entries.map((e) => FlSpot(e.key.toDouble(), e.value)).toList(),
                            color: primary,
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                    SizedBox(
                      height: 200,
                      child: BiBarChart(
                        items: labels.asMap().entries.map((e) => BarChartItem(e.value, counts[e.key], color: tertiary)).toList(),
                      ),
                    ),
                    const SizedBox(height: 16),
                    if (months.isNotEmpty)
                      LayoutBuilder(
                        builder: (_, constraints) => Wrap(
                          spacing: 12, runSpacing: 12,
                          children: [
                            SizedBox(
                              width: (constraints.maxWidth - 12) / 2,
                              child: BiKpiCard(
                                label: 'Total Acumulado',
                                value: '\$${months.fold(0.0, (sum, m) => sum + m.total).toStringAsFixed(0)}',
                                icon: Icons.attach_money,
                                color: primary,
                                sparklineData: totals,
                              ),
                            ),
                            SizedBox(
                              width: (constraints.maxWidth - 12) / 2,
                              child: BiKpiCard(
                                label: 'Total Transacciones',
                                value: '${months.fold(0, (sum, m) => sum + m.count)}',
                                icon: Icons.shopping_cart,
                                color: tertiary,
                                sparklineData: counts,
                              ),
                            ),
                          ],
                        ),
                      ),
                  ],
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}
