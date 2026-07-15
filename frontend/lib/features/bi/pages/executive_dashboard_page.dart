import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fl_chart/fl_chart.dart';
import '../../../core/mixins/auto_refresh_mixin.dart';
import '../../../core/widgets/bi/bi_kpi_card.dart';
import '../../../core/widgets/bi/bi_line_chart.dart';
import '../../../core/widgets/bi/bi_bar_chart.dart';
import '../../../core/widgets/bi/bi_gauge.dart';
import '../providers/bi_provider.dart';
import '../models/bi_models.dart';
import '../../../shared/ds/ds.dart';

class ExecutiveDashboardPage extends ConsumerStatefulWidget {
  const ExecutiveDashboardPage({super.key});

  @override
  ConsumerState<ExecutiveDashboardPage> createState() => _ExecutiveDashboardPageState();
}

class _ExecutiveDashboardPageState extends ConsumerState<ExecutiveDashboardPage> with AutoRefreshMixin<ExecutiveDashboardPage> {
  @override
  void initState() {
    super.initState();
    startAutoRefresh(providers: [
      biProvider,
      biSalesTrendProvider(12),
      biArAgingProvider,
      biFinancialRatiosProvider,
    ]);
  }

  @override
  Widget build(BuildContext context) {
    final executiveAsync = ref.watch(biProvider);
    final salesTrendAsync = ref.watch(biSalesTrendProvider(12));
    final arAgingAsync = ref.watch(biArAgingProvider);
    final ratiosAsync = ref.watch(biFinancialRatiosProvider);

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(biProvider);
          ref.invalidate(biSalesTrendProvider(12));
          ref.invalidate(biArAgingProvider);
          ref.invalidate(biFinancialRatiosProvider);
        },
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            ZAsyncRenderer<BiExecutive>(
              value: executiveAsync,
              builder: (executive) => _buildKpiGrid(context, executive),
            ),
            const SizedBox(height: 24),
            Text('Tendencia de Ventas', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            salesTrendAsync.when(
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
              data: (months) => SizedBox(
                height: 200,
                child: BiLineChart(
                  series: [
                    LineChartSeries(
                      months.asMap().entries.map((e) => FlSpot(e.key.toDouble(), e.value.total)).toList(),
                      color: Theme.of(context).colorScheme.primary,
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),
            Text('Antigüedad de Cartera (AR)', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            arAgingAsync.when(
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
              data: (aging) => SizedBox(
                height: 200,
                child: BiBarChart(
                  items: [
                    BarChartItem('Corriente', aging.current, color: Colors.green),
                    BarChartItem('1-30d', aging.days30, color: Colors.lightGreen),
                    BarChartItem('31-60d', aging.days60, color: Colors.orange),
                    BarChartItem('61-90d', aging.days90, color: Colors.deepOrange),
                    BarChartItem('+90d', aging.days90Plus, color: Colors.red),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),
            Text('Ratios Financieros', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            ratiosAsync.when(
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
              data: (ratios) => LayoutBuilder(
                builder: (ctx, constraints) {
                  final gaugeWidth = ((constraints.maxWidth - 48) / 4).clamp(80.0, 140.0);
                  return Wrap(
                    spacing: 12, runSpacing: 12,
                    children: [
                      SizedBox(width: gaugeWidth, height: gaugeWidth, child: BiGauge(value: ratios.currentRatio, max: 3, label: 'Liquidez', color: ratios.currentRatio >= 1.5 ? Colors.green : Colors.red)),
                      SizedBox(width: gaugeWidth, height: gaugeWidth, child: BiGauge(value: ratios.quickRatio, max: 2, label: 'Prueba Ácida', color: ratios.quickRatio >= 1 ? Colors.green : Colors.red)),
                      SizedBox(width: gaugeWidth, height: gaugeWidth, child: BiGauge(value: ratios.debtRatio, max: 1, label: 'Endeudamiento', color: ratios.debtRatio <= 0.5 ? Colors.green : Colors.red)),
                      SizedBox(width: gaugeWidth, height: gaugeWidth, child: BiGauge(value: ratios.roe, max: 0.5, label: 'ROE', color: ratios.roe >= 0.1 ? Colors.green : Colors.red)),
                    ],
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildKpiGrid(BuildContext context, executive) {
    final theme = Theme.of(context);
    final primary = theme.colorScheme.primary;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Resumen del Día', style: theme.textTheme.titleMedium),
        const SizedBox(height: 8),
        LayoutBuilder(
          builder: (_, constraints) => Wrap(
            spacing: 12, runSpacing: 12,
            children: [
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Ventas Hoy', value: '\$${executive.sales.todaySales.toStringAsFixed(0)}', icon: Icons.trending_up, color: Colors.green, changePercent: executive.sales.salesChangePercent, sparklineData: executive.sales.weeklyTrend)),
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Ticket Promedio', value: '\$${executive.sales.averageTicket.toStringAsFixed(0)}', icon: Icons.receipt, color: primary, sparklineData: [])),
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Ventas del Mes', value: '\$${executive.sales.monthSales.toStringAsFixed(0)}', icon: Icons.calendar_month, color: Colors.blue, changePercent: executive.sales.monthSalesChangePercent, sparklineData: [])),
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Transacciones Hoy', value: '${executive.sales.todaySalesCount}', icon: Icons.shopping_bag, color: Colors.orange, sparklineData: [])),
            ],
          ),
        ),
        const SizedBox(height: 16),
        Text('Créditos', style: theme.textTheme.titleMedium),
        const SizedBox(height: 8),
        LayoutBuilder(
          builder: (_, constraints) => Wrap(
            spacing: 12, runSpacing: 12,
            children: [
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Cartera Total', value: '\$${executive.credits.totalPortfolio.toStringAsFixed(0)}', icon: Icons.account_balance, color: Colors.indigo, sparklineData: [])),
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Créditos Vencidos', value: '${executive.credits.overdueCredits}', icon: Icons.warning, color: Colors.red, changePercent: -executive.credits.collectionRate, sparklineData: [])),
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Recuperación del Mes', value: '\$${executive.credits.monthlyRecovery.toStringAsFixed(0)}', icon: Icons.payments, color: Colors.teal, sparklineData: [])),
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'DSO (días)', value: '${executive.credits.dsoDays.toStringAsFixed(0)}', icon: Icons.schedule, color: Colors.purple, sparklineData: [])),
            ],
          ),
        ),
        const SizedBox(height: 16),
        Text('Inventario', style: theme.textTheme.titleMedium),
        const SizedBox(height: 8),
        LayoutBuilder(
          builder: (_, constraints) => Wrap(
            spacing: 12, runSpacing: 12,
            children: [
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Valor Inventario', value: '\$${executive.inventory.totalStockValue.toStringAsFixed(0)}', icon: Icons.inventory_2, color: Colors.brown, sparklineData: [])),
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Rotación', value: executive.inventory.turnoverRate.toStringAsFixed(1), icon: Icons.repeat, color: Colors.blueGrey, sparklineData: [])),
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Agotados', value: '${executive.inventory.outOfStockProducts}', icon: Icons.block, color: Colors.red, sparklineData: [])),
              SizedBox(width: (constraints.maxWidth - 12) / 2, child: BiKpiCard(label: 'Stock Bajo', value: '${executive.inventory.lowStockProducts}', icon: Icons.inventory, color: Colors.orange, sparklineData: [])),
            ],
          ),
        ),
      ],
    );
  }
}
