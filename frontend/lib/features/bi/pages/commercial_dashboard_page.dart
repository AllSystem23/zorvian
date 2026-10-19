import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fl_chart/fl_chart.dart';
import '../../../core/mixins/auto_refresh_mixin.dart';
import '../../../core/widgets/bi/bi_line_chart.dart';
import '../../../core/widgets/bi/bi_bar_chart.dart';
import '../../../core/widgets/bi/bi_kpi_card.dart';
import '../../../core/widgets/bi/sales_prediction_section.dart';
import '../../../core/widgets/bi/purchase_recommendation_section.dart';
import '../../../core/widgets/bi/expense_classifier_section.dart';
import '../providers/bi_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../../shared/printing/bi_report_actions.dart';
import '../../../shared/printing/bi_report_pdf.dart';

class CommercialDashboardPage extends ConsumerStatefulWidget {
  const CommercialDashboardPage({super.key});

  @override
  ConsumerState<CommercialDashboardPage> createState() => _CommercialDashboardPageState();
}

class _CommercialDashboardPageState extends ConsumerState<CommercialDashboardPage> with AutoRefreshMixin<CommercialDashboardPage> {
  bool _exporting = false;

  @override
  void initState() {
    super.initState();
    startAutoRefresh(providers: [biSalesTrendProvider(12)]);
  }

  /// Genera el reporte comercial profesional (PDF vectorial con tendencia,
  /// transacciones por mes, KPIs, detalle mensual y branding) y lo comparte.
  Future<void> _exportReportPdf() async {
    final trend = ref.read(biSalesTrendProvider(12)).value;
    if (trend == null) {
      _snack('Los datos del dashboard aún no cargan. Intente de nuevo en unos segundos.', error: true);
      return;
    }
    setState(() => _exporting = true);
    try {
      final meta = await buildReportMeta(ref, title: 'Reporte Comercial');
      if (!mounted) return;
      final bytes = await buildCommercialBiReportPdf(trend: trend, meta: meta);
      if (!mounted) return;
      await shareReportPdf(bytes, reportFileName('reporte_comercial'));
      if (mounted) _snack('Reporte PDF generado (${(bytes.length / 1024).toStringAsFixed(0)} KB).');
    } catch (e) {
      if (mounted) _snack('No se pudo generar el reporte: $e', error: true);
    } finally {
      if (mounted) setState(() => _exporting = false);
    }
  }

  void _snack(String message, {bool error = false}) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(message),
      backgroundColor: error ? Theme.of(context).colorScheme.error : null,
    ));
  }

  @override
  Widget build(BuildContext context) {
    final salesTrendAsync = ref.watch(biSalesTrendProvider(12));

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(biSalesTrendProvider(12)),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Dashboard Comercial',
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
                  ),
                ),
                ZButton(
                  text: 'Reporte PDF',
                  icon: Icons.picture_as_pdf,
                  fullWidth: false,
                  isLoading: _exporting,
                  onPressed: _exportReportPdf,
                ),
              ],
            ),
            const SizedBox(height: 16),
            Text('Tendencia de Ventas (12 meses)', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            salesTrendAsync.when(
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
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
                    const SizedBox(height: 24),
                    const SalesPredictionSection(),
                    const SizedBox(height: 24),
                    const PurchaseRecommendationSection(),
                    const SizedBox(height: 24),
                    const ExpenseClassifierSection(),
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
