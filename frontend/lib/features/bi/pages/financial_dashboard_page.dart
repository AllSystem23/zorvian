import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/mixins/auto_refresh_mixin.dart';
import '../../../core/widgets/bi/bi_bar_chart.dart';
import '../../../core/widgets/bi/bi_pie_chart.dart';
import '../../../core/widgets/bi/bi_gauge.dart';
import '../../../core/widgets/bi/anomaly_detection_section.dart';
import '../providers/bi_provider.dart';
import '../models/bi_models.dart';
import '../../../../shared/ds/ds.dart';

class FinancialDashboardPage extends ConsumerStatefulWidget {
  const FinancialDashboardPage({super.key});

  @override
  ConsumerState<FinancialDashboardPage> createState() => _FinancialDashboardPageState();
}
// ... imports
class _FinancialDashboardPageState extends ConsumerState<FinancialDashboardPage> with AutoRefreshMixin<FinancialDashboardPage> {
  @override
  void initState() {
    super.initState();
    startAutoRefresh(providers: [
      biProvider, // Ahora incluimos biProvider para obtener las alertas
      biFinancialRatiosProvider,
      biComparativeIncomeProvider,
      biCashFlowProvider,
      biArAgingProvider,
      biApAgingProvider,
    ]);
  }

  @override
  Widget build(BuildContext context) {
    final ratiosAsync = ref.watch(biFinancialRatiosProvider);
    final comparativeAsync = ref.watch(biComparativeIncomeProvider);
    final cashFlowAsync = ref.watch(biCashFlowProvider);
    final arAgingAsync = ref.watch(biArAgingProvider);
    final apAgingAsync = ref.watch(biApAgingProvider);
    final executiveAsync = ref.watch(biProvider); 

    return Scaffold(
      appBar: AppBar(title: const Text('Panel Financiero')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(biFinancialRatiosProvider);
          ref.invalidate(biComparativeIncomeProvider);
          ref.invalidate(biCashFlowProvider);
          ref.invalidate(biArAgingProvider);
          ref.invalidate(biApAgingProvider);
          ref.invalidate(biProvider);
        },
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // Sección de Alertas
            ZAsyncRenderer<BiExecutive>(
              value: executiveAsync,
              builder: (executive) {
                final alerts = executive.alerts;
                if (alerts.isEmpty) return const SizedBox.shrink();
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Alertas Críticas', style: Theme.of(context).textTheme.titleMedium?.copyWith(color: Colors.red)),
                    const SizedBox(height: 8),
                    ...alerts.map((alert) => ZAlertCard(
                      message: alert.message,
                      severity: alert.severity,
                    )),
                    const SizedBox(height: 16),
                  ],
                );
              },
            ),
            // ... resto de los gráficos ...

            const SizedBox(height: 8),
            ratiosAsync.when(
              loading: () => const SizedBox(height: 150, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
              data: (r) => Wrap(
                spacing: 16, runSpacing: 16,
                children: [
                  SizedBox(width: 120, height: 120, child: BiGauge(value: r.currentRatio, max: 3, label: 'Liquidez', color: r.currentRatio >= 1.5 ? Colors.green : Colors.red)),
                  SizedBox(width: 120, height: 120, child: BiGauge(value: r.quickRatio, max: 2, label: 'Prueba Ácida', color: r.quickRatio >= 1 ? Colors.green : Colors.red)),
                  SizedBox(width: 120, height: 120, child: BiGauge(value: r.debtRatio, max: 1, label: 'Endeudamiento', color: r.debtRatio <= 0.5 ? Colors.green : Colors.red)),
                  SizedBox(width: 120, height: 120, child: BiGauge(value: r.grossMargin, max: 1, label: 'Margen Bruto', color: r.grossMargin >= 0.3 ? Colors.green : Colors.red)),
                  SizedBox(width: 120, height: 120, child: BiGauge(value: r.netMargin, max: 0.5, label: 'Margen Neto', color: r.netMargin >= 0.1 ? Colors.green : Colors.red)),
                  SizedBox(width: 120, height: 120, child: BiGauge(value: r.roe, max: 0.5, label: 'ROE', color: r.roe >= 0.1 ? Colors.green : Colors.red)),
                  SizedBox(width: 120, height: 120, child: BiGauge(value: r.roa, max: 0.3, label: 'ROA', color: r.roa >= 0.05 ? Colors.green : Colors.red)),
                ],
              ),
            ),
            const SizedBox(height: 24),
            Text('Estado de Resultados Comparativo', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            comparativeAsync.when(
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
              data: (ci) => ZCard(
                padding: const EdgeInsets.all(16),
                child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('${ci.current.periodName} vs ${ci.previous.periodName}', style: Theme.of(context).textTheme.bodyMedium),
                      const SizedBox(height: 8),
                      for (final line in ci.changes)
                        Padding(
                          padding: const EdgeInsets.symmetric(vertical: 4),
                          child: Row(
                            children: [
                              Expanded(flex: 2, child: Text(line.name, style: const TextStyle(fontWeight: FontWeight.w500))),
                              Expanded(flex: 1, child: Text('\$${line.currentValue.toStringAsFixed(0)}', textAlign: TextAlign.right)),
                              Expanded(flex: 1, child: Text('\$${line.previousValue.toStringAsFixed(0)}', textAlign: TextAlign.right)),
                              SizedBox(
                                width: 60,
                                child: Text(
                                  '${line.changePercent >= 0 ? '+' : ''}${(line.changePercent * 100).toStringAsFixed(1)}%',
                                  textAlign: TextAlign.right,
                                  style: TextStyle(color: line.changePercent >= 0 ? Colors.green : Colors.red, fontWeight: FontWeight.w600),
                                ),
                              ),
                            ],
                          ),
                        ),
                    ],
                  ),
              ),
            ),
            const SizedBox(height: 24),
            Text('Flujo de Efectivo', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            cashFlowAsync.when(
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
              data: (cf) => SizedBox(
                height: 200,
                child: BiPieChart(
                  items: [
                    PieChartItem('Operación', cf.operating.fold(0.0, (sum, i) => sum + i.amount.abs()), color: Colors.blue),
                    PieChartItem('Inversión', cf.investing.fold(0.0, (sum, i) => sum + i.amount.abs()), color: Colors.orange),
                    PieChartItem('Financiación', cf.financing.fold(0.0, (sum, i) => sum + i.amount.abs()), color: Colors.purple),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),
            Text('Antigüedad AR vs AP', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: arAgingAsync.when(
                    loading: () => const SizedBox(height: 150, child: Center(child: CircularProgressIndicator())),
                    error: (e, _) => ZCard(padding: const EdgeInsets.all(8), child: Text('AR: $e', style: const TextStyle(fontSize: 10))),
                    data: (ar) => Column(
                      children: [
                        const Text('Cuentas por Cobrar', style: TextStyle(fontWeight: FontWeight.w600)),
                        SizedBox(
                          height: 150,
                          child: BiBarChart(
                            items: [
                              BarChartItem('C', ar.current, color: Colors.green),
                              BarChartItem('30', ar.days30, color: Colors.lightGreen),
                              BarChartItem('60', ar.days60, color: Colors.orange),
                              BarChartItem('90', ar.days90, color: Colors.deepOrange),
                              BarChartItem('+90', ar.days90Plus, color: Colors.red),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: apAgingAsync.when(
                    loading: () => const SizedBox(height: 150, child: Center(child: CircularProgressIndicator())),
                    error: (e, _) => ZCard(padding: const EdgeInsets.all(8), child: Text('AP: $e', style: const TextStyle(fontSize: 10))),
                    data: (ap) => Column(
                      children: [
                        const Text('Cuentas por Pagar', style: TextStyle(fontWeight: FontWeight.w600)),
                        SizedBox(
                          height: 150,
                          child: BiBarChart(
                            items: [
                              BarChartItem('C', ap.current, color: Colors.green),
                              BarChartItem('30', ap.days30, color: Colors.lightGreen),
                              BarChartItem('60', ap.days60, color: Colors.orange),
                              BarChartItem('90', ap.days90, color: Colors.deepOrange),
                              BarChartItem('+90', ap.days90Plus, color: Colors.red),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
            const AnomalyDetectionSection(),
          ],
        ),
      ),
    );
  }
}
