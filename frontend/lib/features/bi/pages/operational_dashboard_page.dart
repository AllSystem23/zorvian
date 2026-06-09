import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/mixins/auto_refresh_mixin.dart';
import '../../../core/widgets/bi/bi_bar_chart.dart';
import '../../../core/widgets/bi/bi_pie_chart.dart';
import '../../../core/widgets/bi/bi_kpi_card.dart';
import '../providers/bi_provider.dart';
import '../../../../shared/ds/ds.dart';

class OperationalDashboardPage extends ConsumerStatefulWidget {
  const OperationalDashboardPage({super.key});

  @override
  ConsumerState<OperationalDashboardPage> createState() => _OperationalDashboardPageState();
}

class _OperationalDashboardPageState extends ConsumerState<OperationalDashboardPage> with AutoRefreshMixin<OperationalDashboardPage> {
  @override
  void initState() {
    super.initState();
    startAutoRefresh(providers: [
      biInventorySummaryProvider,
      biPayrollSummaryProvider,
    ]);
  }

  @override
  Widget build(BuildContext context) {
    final inventoryAsync = ref.watch(biInventorySummaryProvider);
    final payrollAsync = ref.watch(biPayrollSummaryProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Panel Operativo')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(biInventorySummaryProvider);
          ref.invalidate(biPayrollSummaryProvider);
        },
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text('Resumen de Inventario', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            inventoryAsync.when(
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
              data: (inv) => Column(
                children: [
                  LayoutBuilder(
                    builder: (_, constraints) => Wrap(
                      spacing: 12, runSpacing: 12,
                      children: [
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Valor Total', value: '\$${inv.totalValue.toStringAsFixed(0)}', icon: Icons.inventory_2, color: Colors.blue, sparklineData: [])),
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Rotación', value: inv.turnoverRate.toStringAsFixed(1), icon: Icons.repeat, color: Colors.teal, sparklineData: [])),
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Stock Bajo', value: '${inv.lowStockCount}', icon: Icons.warning, color: Colors.orange, sparklineData: [])),
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Agotados', value: '${inv.outOfStockCount}', icon: Icons.block, color: Colors.red, sparklineData: [])),
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Total Productos', value: '${inv.totalProducts}', icon: Icons.category, color: Colors.indigo, sparklineData: [])),
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Valor Muerto', value: '\$${inv.deadStockValue.toStringAsFixed(0)}', icon: Icons.delete, color: Colors.grey, sparklineData: [])),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  if (inv.byCategory.isNotEmpty)
                    SizedBox(
                      height: 200,
                      child: BiPieChart(
                        items: [
                          for (int i = 0; i < inv.byCategory.length; i++)
                            PieChartItem(
                              inv.byCategory[i].categoryName,
                              inv.byCategory[i].totalValue.toDouble(),
                              color: [Colors.blue, Colors.red, Colors.green, Colors.orange, Colors.purple, Colors.teal][i % 6],
                            ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 24),
            Text('Resumen de Nómina', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            payrollAsync.when(
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
              data: (pr) => Column(
                children: [
                  LayoutBuilder(
                    builder: (_, constraints) => Wrap(
                      spacing: 12, runSpacing: 12,
                      children: [
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Costo Total', value: '\$${pr.totalCost.toStringAsFixed(0)}', icon: Icons.attach_money, color: Colors.red, sparklineData: [])),
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Promedio/Empleado', value: '\$${pr.averagePerEmployee.toStringAsFixed(0)}', icon: Icons.people, color: Colors.blue, sparklineData: [])),
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Horas Extra', value: '\$${pr.overtimeTotal.toStringAsFixed(0)}', icon: Icons.access_time, color: Colors.orange, sparklineData: [])),
                        SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Costos Patronales', value: '\$${pr.employerCostTotal.toStringAsFixed(0)}', icon: Icons.business, color: Colors.purple, sparklineData: [])),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  if (pr.byDepartment.isNotEmpty)
                    SizedBox(
                      height: 200,
                      child: BiBarChart(
                        items: pr.byDepartment.map((d) => BarChartItem(d.department, d.amount, color: Theme.of(context).colorScheme.primary)).toList(),
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
